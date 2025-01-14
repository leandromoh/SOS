﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json.Serialization;
using Elasticsearch.Net;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using HealthChecks.UI.Client;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog.Web;
using SOS.Lib.Cache;
using SOS.Lib.Cache.Interfaces;
using SOS.Lib.Configuration.ObservationApi;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Database;
using SOS.Lib.Database.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Helpers;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.IO.DwcArchive;
using SOS.Lib.IO.DwcArchive.Interfaces;
using SOS.Lib.IO.Excel;
using SOS.Lib.IO.Excel.Interfaces;
using SOS.Lib.IO.GeoJson;
using SOS.Lib.IO.GeoJson.Interfaces;
using SOS.Lib.JsonConverters;
using SOS.Lib.Managers;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Interfaces;
using SOS.Lib.Models.Processed.Configuration;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.TaxonListService;
using SOS.Lib.Models.TaxonTree;
using SOS.Lib.Repositories.Processed;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Lib.Repositories.Resource;
using SOS.Lib.Repositories.Resource.Interfaces;
using SOS.Lib.Security;
using SOS.Lib.Security.Interfaces;
using SOS.Lib.Services;
using SOS.Lib.Services.Interfaces;
using SOS.Observations.Api.ActionFilters;
using SOS.Observations.Api.ApplicationInsights;
using SOS.Observations.Api.HealthChecks;
using SOS.Observations.Api.Managers;
using SOS.Observations.Api.Managers.Interfaces;
using SOS.Observations.Api.Middleware;
using SOS.Observations.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using DataProviderManager = SOS.Observations.Api.Managers.DataProviderManager;
using IDataProviderManager = SOS.Observations.Api.Managers.Interfaces.IDataProviderManager;


namespace SOS.Observations.Api
{
    /// <summary>
    ///     Program class
    /// </summary>
    public class Startup
    {
        private const string InternalApiName = "InternalSosObservations";
        private const string PublicApiName = "PublicSosObservations";
        private const string InternalApiPrefix = "Internal";

        private bool _isDevelopment;
        /// <summary>
        ///     Start up
        /// </summary>
        /// <param name="env"></param>
        public Startup(IWebHostEnvironment env)
        {
            var environment = env.EnvironmentName.ToLower();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables();

            _isDevelopment = environment.Equals("local");
            if (_isDevelopment)
            {
                // If Development mode, add secrets stored on developer machine 
                // (%APPDATA%\Microsoft\UserSecrets\92cd2cdb-499c-480d-9f04-feaf7a68f89c\secrets.json)
                // In production you should store the secret values as environment variables.
                builder.AddUserSecrets<Startup>();
            }

            Configuration = builder.Build();
        }

        /// <summary>
        ///     Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddResponseCaching();

            services.AddControllers()
                .AddXmlSerializerFormatters()
                .AddJsonOptions(options =>
                {
                    options
.JsonSerializerOptions.Converters.Add(new GeoShapeConverter());
                });

            // MongoDB conventions.
            ConventionRegistry.Register(
                "MongoDB Solution Conventions",
                new ConventionPack
                {
                    new IgnoreExtraElementsConvention(true),
                    new IgnoreIfNullConvention(true)
                },
                t => true);

            // Identity service configuration
            var identityServerConfiguration = Configuration.GetSection("IdentityServer").Get<IdentityServerConfiguration>();

            // Authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = identityServerConfiguration.Audience;
                    options.Authority = identityServerConfiguration.Authority;
                    options.RequireHttpsMetadata = identityServerConfiguration.RequireHttpsMetadata;
                    options.TokenValidationParameters.RoleClaimType = "rname";
                });

            // Add Mvc Core services
            services.AddMvcCore(option => { option.EnableEndpointRouting = false; })
                .AddApiExplorer()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // Add application insights.
            services.AddApplicationInsightsTelemetry(Configuration);
            // Application insights custom
            services.AddApplicationInsightsTelemetryProcessor<IgnoreRequestPathsTelemetryProcessor>();
            services.AddSingleton(Configuration.GetSection("ApplicationInsights").Get<Lib.Configuration.ObservationApi.ApplicationInsights>());
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();

            services.AddApiVersioning(o =>
            {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.ReportApiVersions = true;
                o.ApiVersionReader = new HeaderApiVersionReader("X-Api-Version");
            });

            services.AddVersionedApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;
                });

            var apiVersionDescriptionProvider =
                services.BuildServiceProvider().GetService<IApiVersionDescriptionProvider>();
            services.AddSwaggerGen(
                swagger =>
                {
                    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                    {
                        swagger.SwaggerDoc(
                            $"{InternalApiName}{description.GroupName}",
                            new OpenApiInfo()
                            {
                                Title = $"SOS Observations API (Internal) {description.GroupName.ToUpperInvariant()}",
                                Version = description.ApiVersion.ToString(),
                                Description = "Species Observation System (SOS) - Observations API. Internal API." + (description.IsDeprecated ? " This API version has been deprecated." : "")
                            });

                        swagger.SwaggerDoc(
                            $"{PublicApiName}{description.GroupName}",
                            new OpenApiInfo()
                            {
                                Title = $"SOS Observations API (Public) {description.GroupName.ToUpperInvariant()}",
                                Version = description.ApiVersion.ToString(),
                                Description = "Species Observation System (SOS) - Observations API. Public API." + (description.IsDeprecated ? " This API version has been deprecated." : "")
                            });

                        swagger.CustomOperationIds(apiDesc =>
                        {
                            apiDesc.TryGetMethodInfo(out MethodInfo methodInfo);
                            string controller = apiDesc.ActionDescriptor.RouteValues["controller"];
                            string methodName = methodInfo.Name;
                            return $"{controller}_{methodName}";
                        });
                    }

                    // add a custom operation filter which sets default values
                    swagger.OperationFilter<SwaggerDefaultValues>();

                    var currentAssembly = Assembly.GetExecutingAssembly();
                    var xmlDocs = currentAssembly.GetReferencedAssemblies()
                        .Union(new AssemblyName[] { currentAssembly.GetName() })
                        .Select(a => Path.Combine(Path.GetDirectoryName(currentAssembly.Location), $"{a.Name}.xml"))
                        .Where(f => File.Exists(f)).ToArray();

                    Array.ForEach(xmlDocs, (d) =>
                    {
                        swagger.IncludeXmlComments(d);
                    });

                    swagger.SchemaFilter<SwaggerIgnoreFilter>();
                    // Post-modify Operation descriptions once they've been generated by wiring up one or more
                    // Operation filters.
                    swagger.OperationFilter<ApiManagementDocumentationFilter>();

                    swagger.DocInclusionPredicate((documentName, apiDescription) =>
                    {
                        var apiVersions = GetApiVersions(apiDescription);
                        bool isEndpointInternalApi = apiDescription.ActionDescriptor.EndpointMetadata.Any(x => x.GetType() == typeof(InternalApiAttribute));
                        if (isEndpointInternalApi && !documentName.StartsWith(InternalApiPrefix)) return false;
                        return apiVersions.Any(v =>
                                   $"{InternalApiName}v{v}" == documentName) ||
                               apiVersions.Any(v =>
                                   $"{PublicApiName}v{v}" == documentName);
                    });

                    swagger.AddSecurityDefinition("Bearer", //Name the security scheme
                        new OpenApiSecurityScheme
                        {
                            Name = "Authorization",
                            Description = "JWT Authorization header using the Bearer scheme.",
                            In = ParameterLocation.Header,
                            Type = SecuritySchemeType.Http, //We set the scheme type to http since we're using bearer authentication
                            Scheme = "bearer" //The name of the HTTP Authorization scheme to be used in the Authorization header. In this case "bearer".
                        });

                    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement{
                        {
                            new OpenApiSecurityScheme{
                                Scheme = "bearer",
                                Name = "Bearer",
                                In = ParameterLocation.Header,
                                Reference = new OpenApiReference{
                                    Id = "Bearer", //The name of the previously defined security scheme.
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            new List<string>()
                        }
                    });
                });

            var observationApiConfiguration = Configuration.GetSection("ObservationApiConfiguration")
                .Get<ObservationApiConfiguration>();

            // Hangfire
            var mongoConfiguration = Configuration.GetSection("HangfireDbConfiguration").Get<HangfireDbConfiguration>();
            
            services.AddHangfire(configuration =>
                configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings(m =>
                    {
                        m.Converters.Add(new NewtonsoftGeoShapeConverter());
                        m.Converters.Add(new StringEnumConverter());
                    })
                    .UseMongoStorage(new MongoClient(mongoConfiguration.GetMongoDbSettings()),
                        mongoConfiguration.DatabaseName,
                        new MongoStorageOptions
                        {
                            MigrationOptions = new MongoMigrationOptions
                            {
                                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                                BackupStrategy = new CollectionMongoBackupStrategy()
                            },
                            Prefix = "hangfire",
                            CheckConnection = true
                        })
            );

            //setup the elastic search configuration
            var elasticConfiguration = Configuration.GetSection("SearchDbConfiguration").Get<ElasticSearchConfiguration>();
            services.AddSingleton<IElasticClientManager, ElasticClientManager>(p => new ElasticClientManager(elasticConfiguration));

            // Processed Mongo Db
            var processedDbConfiguration = Configuration.GetSection("ProcessDbConfiguration").Get<MongoDbConfiguration>();
            var processedSettings = processedDbConfiguration.GetMongoDbSettings();
            services.AddScoped<IProcessClient, ProcessClient>(p => new ProcessClient(processedSettings, processedDbConfiguration.DatabaseName,
                processedDbConfiguration.ReadBatchSize, processedDbConfiguration.WriteBatchSize));

            var blobStorageConfiguration = Configuration.GetSection("BlobStorageConfiguration")
                .Get<BlobStorageConfiguration>();

            var healthCheckConfiguration = Configuration.GetSection("HealthCheckConfiguration").Get<HealthCheckConfiguration>();

            // Add configuration
            services.AddSingleton(observationApiConfiguration);
            services.AddSingleton(blobStorageConfiguration);
            services.AddSingleton(elasticConfiguration);
            services.AddSingleton(Configuration.GetSection("UserServiceConfiguration").Get<UserServiceConfiguration>());
            services.AddSingleton(healthCheckConfiguration);
            services.AddSingleton(Configuration.GetSection("VocabularyConfiguration").Get<VocabularyConfiguration>());


            services.AddHealthChecks()
                .AddDiskStorageHealthCheck(
                    x => x.AddDrive("C:\\", (long)(healthCheckConfiguration.MinimumLocalDiskStorage * 1000)),
                    name: $"Primary disk: min {healthCheckConfiguration.MinimumLocalDiskStorage}GB free - warning",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "disk" })
                .AddMongoDb(processedDbConfiguration.GetConnectionString())
                .AddHangfire(a => a
                    .MinimumAvailableServers = 1, "Hangfire", tags: new[] { "hangfire" })
                .AddCheck<DataAmountHealthCheck>("Data amount", tags: new[] { "database", "elasticsearch", "data" })
                .AddCheck<SearchHealthCheck>("Search", tags: new[] { "database", "elasticsearch", "query" })
                .AddCheck<DataProviderHealthCheck>("Data providers", tags: new[] { "data providers", "meta data" })
                .AddCheck<DwcaHealthCheck>("DwC-A files", tags: new[] { "dwca", "export" })
                .AddElasticsearch(a => a
                        .UseServer(string.Join(';', elasticConfiguration.Clusters.Select(c => c.Hosts)))
                        .UseBasicAuthentication(elasticConfiguration.UserName, elasticConfiguration.Password)
                        .UseCertificateValidationCallback((o, certificate, arg3, arg4) => true)
                        .UseCertificateValidationCallback(CertificateValidations.AllowAll), "ElasticSearch", null,
                    tags: new[] { "database", "elasticsearch", "system" });

            // Add security
            services.AddScoped<IAuthorizationProvider, CurrentUserAuthorization>();

            // Add Caches
            services.AddSingleton<IAreaCache, AreaCache>();
            services.AddSingleton<ITaxonObservationCountCache, TaxonObservationCountCache>();
            services.AddSingleton<IDataProviderCache, DataProviderCache>();
            services.AddSingleton<ICache<int, ProjectInfo>, ProjectCache>();
            services.AddSingleton<ICache<VocabularyId, Vocabulary>, VocabularyCache>();
            services.AddSingleton<ICache<int, TaxonList>, TaxonListCache>();
            services.AddSingleton<IClassCache<ProcessedConfiguration>, ClassCache<ProcessedConfiguration>>();
            services.AddSingleton<IClassCache<TaxonTree<IBasicTaxon>>, ClassCache<TaxonTree<IBasicTaxon>>>();
            services.AddSingleton<IClassCache<TaxonListSetsById>, ClassCache<TaxonListSetsById>>();

            // Add managers
            services.AddScoped<IAreaManager, AreaManager>();
            services.AddSingleton<IBlobStorageManager, BlobStorageManager>();
            services.AddScoped<IDataProviderManager, DataProviderManager>();
            services.AddScoped<IDataQualityManager, DataQualityManager>();
            services.AddScoped<IExportManager, ExportManager>();
            services.AddScoped<IFilterManager, FilterManager>();
            services.AddScoped<IObservationManager, ObservationManager>();
            services.AddScoped<IProcessInfoManager, ProcessInfoManager>();
            services.AddScoped<ITaxonListManager, TaxonListManager>();
            services.AddScoped<ITaxonManager, TaxonManager>();
            services.AddScoped<IVocabularyManager, VocabularyManager>();

            // Add repositories
            services.AddScoped<IAreaRepository, AreaRepository>();
            services.AddScoped<IDataProviderRepository, DataProviderRepository>();
            services.AddScoped<IProcessedObservationRepository, ProcessedObservationRepository>();
            services.AddScoped<IProcessInfoRepository, ProcessInfoRepository>();
            services.AddScoped<IProtectedLogRepository, ProtectedLogRepository>();
            services.AddScoped<ITaxonRepository, TaxonRepository>();
            services.AddScoped<IVocabularyRepository, VocabularyRepository>();
            services.AddScoped<IProjectInfoRepository, ProjectInfoRepository>();
            services.AddScoped<ITaxonListRepository, TaxonListRepository>();
            services.AddScoped<IUserExportRepository, UserExportRepository>();

            // Add services
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IHttpClientService, HttpClientService>();
            services.AddSingleton<IUserService, UserService>();

            // Add writers
            services.AddScoped<IDwcArchiveFileWriter, DwcArchiveFileWriter>();
            services.AddScoped<IDwcArchiveFileWriterCoordinator, DwcArchiveFileWriterCoordinator>();
            services.AddScoped<IDwcArchiveEventCsvWriter, DwcArchiveEventCsvWriter>();
            services.AddScoped<IDwcArchiveEventFileWriter, DwcArchiveEventFileWriter>();
            services.AddScoped<IDwcArchiveOccurrenceCsvWriter, DwcArchiveOccurrenceCsvWriter>();
            services.AddScoped<IExtendedMeasurementOrFactCsvWriter, ExtendedMeasurementOrFactCsvWriter>();
            services.AddScoped<ISimpleMultimediaCsvWriter, SimpleMultimediaCsvWriter>();

            services.AddScoped<ICsvFileWriter, CsvFileWriter>();
            services.AddScoped<IExcelFileWriter, ExcelFileWriter>();
            services.AddScoped<IGeoJsonFileWriter, GeoJsonFileWriter>();

            // Helpers, static data => single instance 
            services.AddSingleton<IVocabularyValueResolver, VocabularyValueResolver>();
        }

        /// <summary>
        ///  This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="apiVersionDescriptionProvider"></param>
        /// <param name="configuration"></param>
        /// <param name="applicationInsightsConfiguration"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider apiVersionDescriptionProvider, TelemetryConfiguration configuration, Lib.Configuration.ObservationApi.ApplicationInsights applicationInsightsConfiguration, IProtectedLogRepository protectedLogRepository)
        {

            NLogBuilder.ConfigureNLog($"nlog.{env.EnvironmentName}.config");
            if (_isDevelopment)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
#if DEBUG
            configuration.DisableTelemetry = true;
#endif

            if (applicationInsightsConfiguration.EnableRequestBodyLogging)
            {
                app.UseMiddleware<EnableRequestBufferingMiddelware>();
                app.UseMiddleware<StoreRequestBodyMiddleware>();
            }
            if (applicationInsightsConfiguration.EnableSearchResponseCountLogging)
            {
                app.UseMiddleware<StoreSearchCountMiddleware>();
            }

            app.UseHangfireDashboard();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(options =>
            {
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{InternalApiName}{description.GroupName}/swagger.json",
                        $"SOS Observations API (Internal) {description.GroupName.ToUpperInvariant()}");

                    options.SwaggerEndpoint(
                        $"/swagger/{PublicApiName}{description.GroupName}/swagger.json",
                        $"SOS Observations API (Public) {description.GroupName.ToUpperInvariant()}");

                    options.DisplayOperationId();
                    options.DocExpansion(DocExpansion.None);
                }
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseResponseCaching();
            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(60)
                    };
                context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                    new string[] { "Accept-Encoding" };

                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/health-json", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = async (context, report) =>
                    {
                        var result = JsonConvert.SerializeObject(
                            new
                            {
                                status = report.Status.ToString(),
                                duration = report.TotalDuration,
                                entries = report.Entries.Select(e => new
                                {
                                    key = e.Key,
                                    description = e.Value.Description,
                                    duration = e.Value.Duration,
                                    status = Enum.GetName(typeof(HealthStatus),
                                        e.Value.Status),
                                    tags = e.Value.Tags
                                }).ToList()
                            }, Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        await context.Response.WriteAsync(result);
                    }
                });
            });

            // make sure protected log is created and indexed
            if (protectedLogRepository.VerifyCollectionAsync().Result)
            {
                protectedLogRepository.CreateIndexAsync();
            }
        }

        private static IReadOnlyList<ApiVersion> GetApiVersions(ApiDescription apiDescription)
        {
            var actionApiVersionModel = apiDescription.ActionDescriptor
                .GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);

            var apiVersions = actionApiVersionModel.DeclaredApiVersions.Any()
                ? actionApiVersionModel.DeclaredApiVersions
                : actionApiVersionModel.ImplementedApiVersions;
            return apiVersions;
        }
    }

    /// <summary>
    /// </summary>
    public class AllowAllConnectionsFilter : IDashboardAuthorizationFilter
    {
        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Authorize(DashboardContext context)
        {
            // Allow outside. You need an authentication scenario for this part.
            // DON'T GO PRODUCTION WITH THIS LINES.
            return true;
        }
    }
}