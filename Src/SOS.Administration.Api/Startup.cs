using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SOS.Lib.Configuration.Shared;

namespace SOS.Administration.Api
{
    /// <summary>
    /// Start up class
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Auto fac
        /// </summary>
        public ILifetimeScope AutofacContainer { get; private set; }

        /// <summary>
        /// Start up
        /// </summary>
        /// <param name="env"></param>
        public Startup(IWebHostEnvironment env)
        {
            var environment = env.EnvironmentName.ToLower();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables();
            
            //Add secrets stored on developer machine (%APPDATA%\Microsoft\UserSecrets\92cd2cdb-499c-480d-9f04-feaf7a68f89c\secrets.json)
            if (env.IsDevelopment() ||
                environment == "dev" ||
                environment == "local")
            {
                builder.AddUserSecrets<Startup>();
            }

            Configuration = builder.Build();
        }


        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(x =>
                {
                    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Swagger
            services.AddSwaggerGen(
                options =>
                {
                    options.SwaggerDoc("v1",
                        new OpenApiInfo
                        {
                            Title = "SOS.Administration.Api",
                            Version = "v1",
                            Description = "An API to handle various processing jobs" //,
                            //TermsOfService = "None"
                        });
                    // Set the comments path for the Swagger JSON and UI.
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    Debug.WriteLine(xmlPath);
                    options.IncludeXmlComments(xmlPath);
                });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // Hangfire
            var mongoConfiguration = Configuration.GetSection("ApplicationSettings").GetSection("HangfireDbConfiguration").Get<MongoDbConfiguration>();

            services.AddHangfire(configuration =>
                configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMongoStorage(mongoConfiguration.GetMongoDbSettings(),
                        mongoConfiguration.DatabaseName,
                        new MongoStorageOptions
                        {
                            MigrationOptions = new MongoMigrationOptions
                            {
                                Strategy = MongoMigrationStrategy.Migrate,
                                BackupStrategy = MongoBackupStrategy.Collections
                            }
                        })
            );

        }

        /// <summary>
        /// Register Autofac services. This runs after ConfigureServices so the things
        /// here will override registrations made in ConfigureServices.
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline. 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireDashboard("/hangfire", new DashboardOptions()
            {
                Authorization = new[] { new AllowAllConnectionsFilter() },
                IgnoreAntiforgeryToken = true
            });


            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ObservationProcessingJobs API, version 1");
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static MongoStorageOptions MongoStorageOptions
        {
            get
            {
                var migrationOptions = new MongoMigrationOptions
                {
                    Strategy = MongoMigrationStrategy.Migrate,
                    BackupStrategy = MongoBackupStrategy.Collections
                };

                var storageOptions = new MongoStorageOptions { MigrationOptions = migrationOptions };
                return storageOptions;
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class AllowAllConnectionsFilter : IDashboardAuthorizationFilter
    {
        /// <summary>
        /// 
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