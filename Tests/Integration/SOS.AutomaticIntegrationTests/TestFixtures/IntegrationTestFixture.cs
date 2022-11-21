﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson.Serialization.Conventions;
using Moq;
using SOS.Harvest.Extensions;
using SOS.Harvest.Managers;
using SOS.Harvest.Processors.Artportalen;
using SOS.Harvest.Processors.DarwinCoreArchive;
using SOS.Lib.Cache;
using SOS.Lib.Cache.Interfaces;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Database;
using SOS.Lib.Database.Interfaces;
using SOS.Lib.Extensions;
using SOS.Lib.Factories;
using SOS.Lib.Helpers;
using SOS.Lib.IO.DwcArchive;
using SOS.Lib.IO.Excel;
using SOS.Lib.IO.GeoJson;
using SOS.Lib.Managers;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Interfaces;
using SOS.Lib.Models.Processed.Checklist;
using SOS.Lib.Models.Processed.Configuration;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search.Result;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.TaxonListService;
using SOS.Lib.Models.TaxonTree;
using SOS.Lib.Models.UserService;
using SOS.Lib.Models.Verbatim.Artportalen;
using SOS.Lib.Models.Verbatim.DarwinCore;
using SOS.Lib.Repositories.Processed;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Lib.Repositories.Resource;
using SOS.Lib.Repositories.Verbatim;
using SOS.Lib.Security;
using SOS.Lib.Security.Interfaces;
using SOS.Lib.Services;
using SOS.Lib.Services.Interfaces;
using SOS.Observations.Api.Configuration;
using SOS.Observations.Api.Controllers;
using SOS.Observations.Api.HealthChecks;
using SOS.Observations.Api.Managers;
using SOS.Observations.Api.Managers.Interfaces;
using SOS.Observations.Api.Repositories;
using SOS.TestHelpers;
using DataProviderManager = SOS.Observations.Api.Managers.DataProviderManager;

namespace SOS.AutomaticIntegrationTests.TestFixtures
{
    public class IntegrationTestFixture : FixtureBase, IDisposable
    {
        public ArtportalenObservationProcessor ArtportalenObservationProcessor { get; set; }
        public ArtportalenObservationFactory ArtportalenObservationFactory { get; set; }
        public ArtportalenChecklistFactory ArtportalenChecklistFactory { get; set; }
        public InstallationEnvironment InstallationEnvironment { get; private set; }
        public ObservationsController ObservationsController { get; private set; }
        public ChecklistsController ChecklistsController { get; private set; }
        public ExportsController ExportsController { get; private set; }
        public SystemsController SystemsController { get; private set; }
        public VocabulariesController VocabulariesController { get; private set; }
        public UserController UserController { get; private set; }
        public DataProvidersController DataProvidersController { get; private set; }
        public IProcessedObservationCoreRepository ProcessedObservationRepository { get; set; }
        public ProcessedChecklistRepository ProcessedChecklistRepository { get; set; }
        public UserObservationRepository UserObservationRepository { get; set; }
        public ArtportalenVerbatimRepository ArtportalenVerbatimRepository { get; set; }
        public ArtportalenChecklistVerbatimRepository ArtportalenChecklistVerbatimRepository { get; set; }
        private DwcaObservationFactory _darwinCoreFactory;
        private IUserService _userService;

        public DwcaObservationFactory GetDarwinCoreFactory(bool initAreaHelper)
        {            
            if (_darwinCoreFactory == null)
            {
                var dataProvider = new DataProvider() { Id = 1, Identifier = "Artportalen" };
                _areaHelper = new AreaHelper(new AreaRepository(_processClient, new NullLogger<AreaRepository>()));                
                _darwinCoreFactory = CreateDarwinCoreFactoryAsync(dataProvider).Result;
            }

            if (initAreaHelper && !_areaHelper.IsInitialized)
            {
                _areaHelper.InitializeAsync().Wait();
            }

            return _darwinCoreFactory;
            
        }

        public DarwinCoreArchiveVerbatimRepository GetDarwinCoreArchiveVerbatimRepository(DataProvider dataProvider)
        {
            return new DarwinCoreArchiveVerbatimRepository(dataProvider,
                _importClient,
                new Mock<ILogger>().Object);
        }
        public async Task<DwcaObservationFactory> CreateDarwinCoreFactoryAsync(DataProvider dataProvider)
        {
            var factory = await DwcaObservationFactory.CreateAsync(
                dataProvider,
                _taxaById,
                _vocabularyRepository,
                _areaHelper,
                _processTimeManager,
                _processConfiguration);

            return factory;                
        }

        public DwcArchiveFileWriter DwcArchiveFileWriter { get; set; }
        private IFilterManager _filterManager;
        private IUserManager _userManager;
        private VerbatimClient _importClient;
        public List<Taxon> Taxa;
        private Dictionary<int, Taxon> _taxaById;
        private VocabularyRepository _vocabularyRepository;
        private ProcessClient _processClient;
        private AreaHelper _areaHelper;
        private ProcessTimeManager _processTimeManager;
        private ProcessConfiguration _processConfiguration;
        private VocabularyValueResolver _vocabularyValueResolver;
        public string UserAuthenticationToken { get; set; }
        public TaxonManager TaxonManager { get; private set; }
        public SearchDataProvidersHealthCheck SearchDataProvidersHealthCheck { get; set; }
        public SearchPerformanceHealthCheck SearchPerformanceHealthCheck { get; set; }
        public AzureSearchHealthCheck AzureSearchHealthCheck { get; set; }
        public DwcArchiveOccurrenceCsvWriter DwcArchiveOccurrenceCsvWriter { get; set; }
        public IntegrationTestFixture()
        {
            // MongoDB conventions.
            ConventionRegistry.Register(
                "MongoDB Solution Conventions",
                new ConventionPack
                {
                    new IgnoreExtraElementsConvention(true),
                    new IgnoreIfNullConvention(true)
                },
                t => true);

            InstallationEnvironment = GetEnvironmentFromAppSettings();
            Initialize().Wait();
            CreateObservationIntegrationTestIndexAsync(false).Wait();
            CreateObservationIntegrationTestIndexAsync(true).Wait();
            CreateChecklistIntegrationTestIndexAsync().Wait();
            CreateUserObservationIntegrationTestIndexAsync().Wait();
        }

        public void Dispose()
        {            
            DeleteObservationIntegrationTestIndexAsync(false).Wait();
            DeleteObservationIntegrationTestIndexAsync(true).Wait();
            DeleteChecklistIntegrationTestIndexAsync().Wait();
            DeleteUserObservationIntegrationTestIndexAsync().Wait();
        }

        public void InitControllerHttpContext()
        {
            ExportsController.ControllerContext.HttpContext = new DefaultHttpContext();
        }

        protected string GetUserAuthenticationToken()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var userAuthenticationToken = config.GetSection($"{configPrefix}:UserAuthenticationToken").Get<string>();
            return userAuthenticationToken;
        }

        protected bool GetUseTaxonZipCollection()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var useTaxonZipCollection = config.GetSection($"{configPrefix}:UseTaxonZipCollection").Get<bool>();
            return useTaxonZipCollection;
        }

        protected string GetAzureApiUrl()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var azureApiUrl = config.GetSection($"{configPrefix}:AzureApiUrl").Get<string>();
            return azureApiUrl;
        }

        protected string GetAzureApiSubscriptionKey()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var azureApiUrl = config.GetSection($"{configPrefix}:AzureApiSubscriptionKey").Get<string>();
            return azureApiUrl;
        }

        protected MongoDbConfiguration GetMongoDbConfiguration()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var mongoDbConfiguration = config.GetSection($"{configPrefix}:ProcessDbConfiguration").Get<MongoDbConfiguration>();
            return mongoDbConfiguration;
        }

        protected MongoDbConfiguration GetVerbatimMongoDbConfiguration()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var mongoDbConfiguration = config.GetSection($"{configPrefix}:VerbatimDbConfiguration").Get<MongoDbConfiguration>();
            return mongoDbConfiguration;
        }

        protected ElasticSearchConfiguration GetSearchDbConfiguration()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var elasticConfiguration = config.GetSection($"{configPrefix}:SearchDbConfiguration").Get<ElasticSearchConfiguration>();
            return elasticConfiguration;
        }

        protected ElasticSearchConfiguration GetCustomSearchDbConfiguration()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var elasticConfiguration = config.GetSection($"{configPrefix}:CustomSearchDbConfiguration").Get<ElasticSearchConfiguration>();
            return elasticConfiguration;
        }

        protected UserServiceConfiguration GetUserServiceConfiguration()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var userServiceConfiguration = config.GetSection($"{configPrefix}:UserServiceConfiguration").Get<UserServiceConfiguration>();
            return userServiceConfiguration;
        }

        protected ObservationApiConfiguration GetObservationApiConfiguration()
        {
            var config = GetAppSettings();
            var configPrefix = GetConfigPrefix(InstallationEnvironment);
            var observationApiConfiguration = config.GetSection($"{configPrefix}:ObservationApiConfiguration").Get<ObservationApiConfiguration>();
            return observationApiConfiguration;
        }

        private async Task Initialize()
        {
            UserAuthenticationToken = GetUserAuthenticationToken();
            ElasticSearchConfiguration elasticConfiguration = GetSearchDbConfiguration();
            if (!elasticConfiguration.IndexPrefix.Contains("integrationtests"))
                throw new Exception("Elasticsearch configuration must use integrationtest index");
            var blobStorageManagerMock = new Mock<IBlobStorageManager>();
            var observationApiConfiguration = GetObservationApiConfiguration();
            var elasticClientManager = new ElasticClientManager(elasticConfiguration);
            var mongoDbConfiguration = GetMongoDbConfiguration();
            var processedSettings = mongoDbConfiguration.GetMongoDbSettings();
            _processClient = new ProcessClient(processedSettings, mongoDbConfiguration.DatabaseName,
                mongoDbConfiguration.ReadBatchSize, mongoDbConfiguration.WriteBatchSize);
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var areaManager = CreateAreaManager(_processClient);

            // Taxonomy
            var taxonRepository = new TaxonRepository(_processClient, new NullLogger<TaxonRepository>());
            bool useTaxonZipCollection = GetUseTaxonZipCollection();
            if (useTaxonZipCollection)
            {
                Taxa = GetTaxaFromZipFile();
            }
            else
            {
                Taxa = await taxonRepository.GetAllAsync();
            }
            _taxaById = Taxa.ToDictionary(m => m.Id, m => m);
            TaxonTree<IBasicTaxon>? basicTaxonTree = TaxonTreeFactory.CreateTaxonTree(Taxa.ToDictionary(t => t.Id, t => t));
            var taxonTreeCache = new ClassCache<TaxonTree<IBasicTaxon>>(memoryCache);
            taxonTreeCache.Set(basicTaxonTree);
            var taxonManager = CreateTaxonManager(_processClient, taxonRepository, memoryCache, taxonTreeCache);

            var processedObservationRepository = CreateProcessedObservationRepository(elasticConfiguration, elasticClientManager, _processClient, memoryCache, taxonManager);
            ProcessedChecklistRepository = CreateProcessedChecklistRepository(elasticConfiguration, elasticClientManager, _processClient);
            UserObservationRepository = CreateUserObservationRepository(elasticConfiguration, elasticClientManager, _processClient);
            _vocabularyRepository = new VocabularyRepository(_processClient, new NullLogger<VocabularyRepository>());
            var vocabularyManger = CreateVocabularyManager(_processClient, _vocabularyRepository);
            var projectManger = CreateProjectManager(_processClient);
            var processInfoRepository = new ProcessInfoRepository(_processClient, new NullLogger<ProcessInfoRepository>());
            var processInfoManager = new ProcessInfoManager(processInfoRepository, new NullLogger<ProcessInfoManager>());
            var dataProviderCache = new DataProviderCache(new DataProviderRepository(_processClient, new NullLogger<DataProviderRepository>()));
            var dataproviderManager = new DataProviderManager(dataProviderCache, processInfoManager, processedObservationRepository, new NullLogger<DataProviderManager>());
            var fileService = new FileService();
            _vocabularyValueResolver = new VocabularyValueResolver(_vocabularyRepository, new VocabularyConfiguration { ResolveValues = true, LocalizationCultureCode = "sv-SE" });
            var csvFileWriter = new CsvFileWriter(processedObservationRepository, fileService,
                _vocabularyValueResolver, new NullLogger<CsvFileWriter>());
            var dwcArchiveFileWriter = CreateDwcArchiveFileWriter(_vocabularyValueResolver, _processClient);
            var excelFileWriter = new ExcelFileWriter(processedObservationRepository, fileService,
                _vocabularyValueResolver, new NullLogger<ExcelFileWriter>());
            var geojsonFileWriter = new GeoJsonFileWriter(processedObservationRepository, fileService,
                _vocabularyValueResolver, new NullLogger<GeoJsonFileWriter>());
            var areaRepository = new AreaRepository(_processClient, new NullLogger<AreaRepository>());
            var areaCache = new AreaCache(areaRepository);
            _userService = CreateUserService();
            var filterManager = new FilterManager(taxonManager, _userService, areaCache, dataProviderCache);
            _filterManager = filterManager;
            var observationManager = CreateObservationManager(processedObservationRepository, _vocabularyValueResolver, _processClient, filterManager);
            var taxonProcessedTaxonRepository = CreateProcessedTaxonRepository(elasticConfiguration, elasticClientManager, _processClient, taxonManager);
            var taxonSearchManager = CreateTaxonSearchManager(taxonProcessedTaxonRepository, filterManager);
            var exportManager = new ExportManager(csvFileWriter, dwcArchiveFileWriter, excelFileWriter, geojsonFileWriter,
                processedObservationRepository, processInfoRepository, filterManager, new NullLogger<ExportManager>());
            var userExportRepository = new UserExportRepository(_processClient, new NullLogger<UserExportRepository>());
            ObservationsController = new ObservationsController(observationManager, taxonSearchManager, taxonManager, areaManager, observationApiConfiguration, elasticConfiguration, new NullLogger<ObservationsController>());
            var checklistManager = new ChecklistManager(ProcessedChecklistRepository, processedObservationRepository, filterManager, new NullLogger<ChecklistManager>());
            ChecklistsController = new ChecklistsController(checklistManager, taxonManager, new NullLogger<ChecklistsController>());
            VocabulariesController = new VocabulariesController(vocabularyManger, projectManger, new NullLogger<VocabulariesController>());
            DataProvidersController = new DataProvidersController(dataproviderManager, 
                observationManager,
                processInfoManager,
                processedObservationRepository,
                new NullLogger<DataProvidersController>());
            TaxonManager = taxonManager;
            ProcessedObservationRepository = processedObservationRepository;
            ExportsController = new ExportsController(observationManager, blobStorageManagerMock.Object, areaManager,
                taxonManager, exportManager, fileService, userExportRepository, observationApiConfiguration,
                new NullLogger<ExportsController>());
            ExportsController.ControllerContext.HttpContext = new DefaultHttpContext();
            DwcArchiveFileWriter = dwcArchiveFileWriter;
            var healthCheckConfiguration = new HealthCheckConfiguration
            {
                AzureApiUrl = GetAzureApiUrl(),
                AzureSubscriptionKey = GetAzureApiSubscriptionKey()
            };
            SearchDataProvidersHealthCheck = new SearchDataProvidersHealthCheck(observationManager, dataProviderCache);
            SearchPerformanceHealthCheck = new SearchPerformanceHealthCheck(observationManager);
            AzureSearchHealthCheck = new AzureSearchHealthCheck(healthCheckConfiguration, new NullLogger<AzureSearchHealthCheck>());
            SystemsController = new SystemsController(processInfoManager, processedObservationRepository, new NullLogger<SystemsController>());
            _userManager = new UserManager(_userService, areaCache, new NullLogger<UserManager>());
            UserController = new UserController(_userManager, new NullLogger<UserController>());
            var artportalenDataProvider = new Lib.Models.Shared.DataProvider { Id = 1 };

            _processConfiguration = new ProcessConfiguration();
            _processTimeManager = new ProcessTimeManager(_processConfiguration);            
            ArtportalenObservationFactory = await ArtportalenObservationFactory.CreateAsync(
                artportalenDataProvider,
                _taxaById,
                _vocabularyRepository,
                false,
                "https://www.artportalen.se",
                _processTimeManager,
                _processConfiguration);
            ArtportalenChecklistFactory = new ArtportalenChecklistFactory(artportalenDataProvider, _processTimeManager, _processConfiguration);
            var verbatimDbConfiguration = GetVerbatimMongoDbConfiguration();
            _importClient = new VerbatimClient(
                verbatimDbConfiguration.GetMongoDbSettings(),
                verbatimDbConfiguration.DatabaseName,
                verbatimDbConfiguration.ReadBatchSize,
                verbatimDbConfiguration.WriteBatchSize);
            ArtportalenVerbatimRepository = new ArtportalenVerbatimRepository(_importClient, new NullLogger<ArtportalenVerbatimRepository>());
            ArtportalenChecklistVerbatimRepository = new ArtportalenChecklistVerbatimRepository(_importClient, new NullLogger<ArtportalenChecklistVerbatimRepository>());
            DwcArchiveOccurrenceCsvWriter = new DwcArchiveOccurrenceCsvWriter(_vocabularyValueResolver, new NullLogger<DwcArchiveOccurrenceCsvWriter>());
        }

        private List<Taxon> GetTaxaFromZipFile()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(assemblyPath, @"Resources\TaxonCollection.zip");

            using (ZipArchive archive = ZipFile.OpenRead(filePath))
            {
                var taxonFile = archive.Entries.FirstOrDefault(f =>
                    f.Name.Equals("TaxonCollection.json", StringComparison.CurrentCultureIgnoreCase));

                var taxonFileStream = taxonFile.Open();
                using var sr = new StreamReader(taxonFileStream, Encoding.UTF8);
                string strJson = sr.ReadToEnd();                
                var jsonSerializerOptions = new System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true };                            
                var taxa = System.Text.Json.JsonSerializer.Deserialize<List<Taxon>>(strJson, jsonSerializerOptions);
                return taxa;
            }
        }

        private DwcArchiveFileWriter CreateDwcArchiveFileWriter(VocabularyValueResolver vocabularyValueResolver, ProcessClient processClient)
        {
            var dwcArchiveFileWriter = new DwcArchiveFileWriter(
                new DwcArchiveOccurrenceCsvWriter(vocabularyValueResolver,
                    new NullLogger<DwcArchiveOccurrenceCsvWriter>()),
                new ExtendedMeasurementOrFactCsvWriter(new NullLogger<ExtendedMeasurementOrFactCsvWriter>()),
                new SimpleMultimediaCsvWriter(new NullLogger<SimpleMultimediaCsvWriter>()),
                new FileService(), new DataProviderRepository(processClient, new NullLogger<DataProviderRepository>()),
                new NullLogger<DwcArchiveFileWriter>());

            return dwcArchiveFileWriter;
        }

        private AreaManager CreateAreaManager(ProcessClient processClient)
        {
            var areaRepository = new AreaRepository(processClient, new NullLogger<AreaRepository>());
            var areaCache = new AreaCache(areaRepository);
            var areaManager = new AreaManager(areaCache, new NullLogger<AreaManager>());
            return areaManager;
        }

        private TaxonManager CreateTaxonManager(ProcessClient processClient, 
            TaxonRepository taxonRepository, 
            IMemoryCache memoryCache, 
            IClassCache<TaxonTree<IBasicTaxon>> taxonTreeCache)
        {            
            var taxonListRepository = new TaxonListRepository(processClient, new NullLogger<TaxonListRepository>());
            var taxonManager = new TaxonManager(taxonRepository, taxonListRepository,
                taxonTreeCache,
                new ClassCache<TaxonListSetsById>(memoryCache),
                new NullLogger<TaxonManager>());
            return taxonManager;
        }

        private ObservationManager CreateObservationManager(
            ProcessedObservationRepository processedObservationRepository,
            VocabularyValueResolver vocabularyValueResolver,
            ProcessClient processClient,
            FilterManager filterManager)
        {
            var protectedLogRepository = new ProtectedLogRepository(processClient, new NullLogger<ProtectedLogRepository>());
            MemoryCacheOptions memoryCacheOptions = new MemoryCacheOptions { SizeLimit = null };
            var artportalenApiService = new ArtportalenApiService(
                new HttpClientService(new NullLogger<HttpClientService>()),
                new ArtportalenApiServiceConfiguration { BaseAddress = "https://api.artdata.slu.se/observations/v2", AcceptHeaderContentType = "application/json" },
                new NullLogger<ArtportalenApiService>());
         
            var observationsManager = new ObservationManager(processedObservationRepository,
                protectedLogRepository,
                vocabularyValueResolver,
                filterManager,
                new HttpContextAccessor(),
                new TaxonObservationCountCache(),
                new ClassCache<Dictionary<int, TaxonSumAggregationItem>>(new MemoryCache(new MemoryCacheOptions())) { CacheDuration = TimeSpan.FromHours(4) },
                new NullLogger<ObservationManager>());

            return observationsManager;
        }

        private TaxonSearchManager CreateTaxonSearchManager(
           ProcessedTaxonRepository processedTaxonRepository,
           FilterManager filterManager)
        {
            var taxonSearchManager = new TaxonSearchManager(processedTaxonRepository,
                filterManager,
                new ClassCache<Dictionary<int, TaxonSumAggregationItem>>(new MemoryCache(new MemoryCacheOptions())) { CacheDuration = TimeSpan.FromHours(4) },
                new NullLogger<TaxonSearchManager>());

            return taxonSearchManager;
        }

        protected virtual IUserService CreateUserService()
        {
            var userServiceConfiguration = GetUserServiceConfiguration();
            var userService = new UserService(new Mock<IAuthorizationProvider>().Object,
                new HttpClientService(new NullLogger<HttpClientService>()), userServiceConfiguration, new NullLogger<UserService>());
            return userService;
        }

        protected virtual IUserService CreateUserService(string token)
        {
            var userServiceConfiguration = GetUserServiceConfiguration();
            IHttpContextAccessor contextAccessor = new HttpContextAccessor();
            contextAccessor.HttpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Request.Headers.Add("Authorization", token);
            IAuthorizationProvider authorizationProvider = new CurrentUserAuthorization(contextAccessor);
            var userService = new UserService(authorizationProvider,
                new HttpClientService(new NullLogger<HttpClientService>()), userServiceConfiguration, new NullLogger<UserService>());
            return userService;
        }

        private VocabularyManager CreateVocabularyManager(ProcessClient processClient, VocabularyRepository vocabularyRepository)
        {
            var vocabularyCache = new VocabularyCache(vocabularyRepository);
            var projectInfoRepository = new ProjectInfoRepository(processClient, new NullLogger<ProjectInfoRepository>());
            var projectInfoCache = new ProjectCache(projectInfoRepository);
            var vocabularyManager = new VocabularyManager(vocabularyCache, new NullLogger<VocabularyManager>());
            return vocabularyManager;
        }

        private ProjectManager CreateProjectManager(ProcessClient processClient)
        {
            var projectInfoRepository = new ProjectInfoRepository(processClient, new NullLogger<ProjectInfoRepository>());
            var projectInfoCache = new ProjectCache(projectInfoRepository);
            var projectManager = new ProjectManager(projectInfoCache, new NullLogger<ProjectManager>());
            return projectManager;
        }
        
        private ProcessedObservationRepository CreateProcessedObservationRepository(
            ElasticSearchConfiguration elasticConfiguration,
            IElasticClientManager elasticClientManager,
            IProcessClient processClient,
            IMemoryCache memoryCache,
            ITaxonManager taxonManager)
        {
            var processedConfigurationCache = new ClassCache<ProcessedConfiguration>(memoryCache);
            var processedObservationRepository = new ProcessedObservationRepository(
                elasticClientManager,
                new ProcessedConfigurationCache(new ProcessedConfigurationRepository(processClient, new NullLogger<ProcessedConfigurationRepository>())),
                new TelemetryClient(),
                elasticConfiguration,
                new NullLogger<ProcessedObservationRepository>());
            return processedObservationRepository;
        }

        private ProcessedTaxonRepository CreateProcessedTaxonRepository(
            ElasticSearchConfiguration elasticConfiguration,
            IElasticClientManager elasticClientManager,
            IProcessClient processClient,
            ITaxonManager taxonManager)
        {
            var processedTaxonRepository = new ProcessedTaxonRepository(
                elasticClientManager,
                elasticConfiguration,
                new ProcessedConfigurationCache(new ProcessedConfigurationRepository(processClient, new NullLogger<ProcessedConfigurationRepository>())),
                taxonManager,
                new NullLogger<ProcessedTaxonRepository>());
            return processedTaxonRepository;
        }

        private ProcessedChecklistRepository CreateProcessedChecklistRepository(
            ElasticSearchConfiguration elasticConfiguration,
            IElasticClientManager elasticClientManager,
            IProcessClient processClient)
        {            
            var processedChecklistRepository = new ProcessedChecklistRepository(
                elasticClientManager,
                elasticConfiguration,
                new ProcessedConfigurationCache(new ProcessedConfigurationRepository(processClient, new NullLogger<ProcessedConfigurationRepository>())),                
                new NullLogger<ProcessedChecklistRepository>());
            
            return processedChecklistRepository;
        }

        private UserObservationRepository CreateUserObservationRepository(
            ElasticSearchConfiguration elasticConfiguration,
            IElasticClientManager elasticClientManager,
            IProcessClient processClient)
        {
            var userObservationRepository = new UserObservationRepository(
                elasticClientManager,
                elasticConfiguration,
                new ProcessedConfigurationCache(new ProcessedConfigurationRepository(processClient, new NullLogger<ProcessedConfigurationRepository>())),
                new NullLogger<UserObservationRepository>());

            return userObservationRepository;
        }

        public void UseMockUserService(int userId, params AuthorityModel[] authorities)
        {
            UserModel user = new UserModel();
            user.Id = userId;
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(userService => userService.GetUserAsync())
                .ReturnsAsync(user);
            userServiceMock.Setup(userService =>
                    userService.GetUserAuthoritiesAsync(userId, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authorities);
            _filterManager.UserService = userServiceMock.Object;  
        }

        public void UseMockUser(ControllerBase controller, int userId, string email)
        {
            var contextAccessor = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim("scope", "SOS.Observations.Protected"));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, email));
            contextAccessor.HttpContext.User.AddIdentity(claimsIdentity);

            controller.ControllerContext.HttpContext = new DefaultHttpContext { User = contextAccessor.HttpContext.User };
        }

        public void RestoreUserService()
        {
            _filterManager.UserService = _userService;
        }

        public void UseUserServiceWithToken(string token)
        {
            var userService = CreateUserService(token);
            _filterManager.UserService = userService;
            _userManager.UserService = userService;
        }

        private async Task CreateObservationIntegrationTestIndexAsync(bool protectedIndex)
        {            
            await ProcessedObservationRepository.ClearCollectionAsync(protectedIndex);
        }

        private async Task DeleteObservationIntegrationTestIndexAsync(bool protectedIndex)
        {
            await ProcessedObservationRepository.DeleteCollectionAsync(protectedIndex);
        }

        private async Task CreateChecklistIntegrationTestIndexAsync()
        {
            await ProcessedChecklistRepository.ClearCollectionAsync();
        }

        private async Task DeleteChecklistIntegrationTestIndexAsync()
        {
            await ProcessedChecklistRepository.DeleteCollectionAsync();
        }

        private async Task CreateUserObservationIntegrationTestIndexAsync()
        {
            await UserObservationRepository.ClearCollectionAsync();
        }
        private async Task DeleteUserObservationIntegrationTestIndexAsync()
        {
            await UserObservationRepository.DeleteCollectionAsync();
        }

        public async Task ProcessAndAddObservationsToElasticSearch(IEnumerable<ArtportalenObservationVerbatim> verbatimObservations)
        {
            var processedObservations = ProcessObservations(verbatimObservations);
            await AddObservationsToElasticsearchAsync(processedObservations);
        }

        public async Task ProcessAndAddUserObservationToElasticSearch(IEnumerable<ArtportalenObservationVerbatim> verbatimObservations)
        {
            List<Observation> processedObservations = ProcessObservations(verbatimObservations);
            var userObservations = processedObservations.ToUserObservations();
            await AddUserObservationsToElasticsearchAsync(userObservations);
        }

        public async Task ProcessAndAddChecklistsToElasticSearch(IEnumerable<ArtportalenChecklistVerbatim> verbatimChecklists)
        {
            var processedChecklists = ProcessChecklists(verbatimChecklists);
            await AddChecklistsToElasticsearchAsync(processedChecklists);
        }

        public async Task AddObservationsToElasticsearchAsync(IEnumerable<Observation> observations, bool clearExistingObservations = true)
        {
            var publicObservations = new List<Observation>();
            var protectedObservations = new List<Observation>();

            foreach(var observation in observations)
            {
                if (observation.ShallBeProtected())
                {
                    protectedObservations.Add(observation);
                }
                else
                {
                    publicObservations.Add(observation);
                }
            }

            await AddObservationsBatchToElasticsearchAsync(publicObservations, false, clearExistingObservations);
            await AddObservationsBatchToElasticsearchAsync(protectedObservations, true, clearExistingObservations);
            
            Thread.Sleep(1000);
        }

        private async Task AddObservationsBatchToElasticsearchAsync(IEnumerable<Observation> observations, 
            bool protectedIndex,
            bool clearExistingObservations = true)
        {            
            if (clearExistingObservations)
            {
                await ProcessedObservationRepository.DeleteAllDocumentsAsync(protectedIndex);
            }
            await ProcessedObservationRepository.DisableIndexingAsync(protectedIndex);
            await ProcessedObservationRepository.AddManyAsync(observations, protectedIndex);
            await ProcessedObservationRepository.EnableIndexingAsync(protectedIndex);            
        }

        public async Task AddChecklistsToElasticsearchAsync(IEnumerable<Checklist> checklists, bool clearExistingChecklists = true)
        {
            if (clearExistingChecklists)
            {
                await ProcessedChecklistRepository.DeleteAllDocumentsAsync();
            }
            await ProcessedChecklistRepository.DisableIndexingAsync();
            await ProcessedChecklistRepository.AddManyAsync(checklists);
            await ProcessedChecklistRepository.EnableIndexingAsync();

            Thread.Sleep(1000);
        }

        public async Task AddUserObservationsToElasticsearchAsync(IEnumerable<UserObservation> userObservations, bool clearExistingUserObservations = true)
        {
            if (clearExistingUserObservations)
            {
                await UserObservationRepository.DeleteAllDocumentsAsync();
            }
            await UserObservationRepository.DisableIndexingAsync();
            await UserObservationRepository.AddManyAsync(userObservations);
            await UserObservationRepository.EnableIndexingAsync();

            Thread.Sleep(1000);
        }

        public List<Observation> ProcessObservations(IEnumerable<ArtportalenObservationVerbatim> verbatimObservations)
        {
            var processedObservations = new List<Observation>();
            bool diffuseIfSupported = false;
            foreach (var verbatimObservation in verbatimObservations)
            {
                var processedObservation = ArtportalenObservationFactory.CreateProcessedObservation(verbatimObservation, diffuseIfSupported);
                processedObservations.Add(processedObservation);
            }

            _vocabularyValueResolver.ResolveVocabularyMappedValues(processedObservations, true);
            return processedObservations;
        }


        public List<Checklist> ProcessChecklists(IEnumerable<ArtportalenChecklistVerbatim> verbatimChecklists)
        {            
            var checklists = new List<Checklist>();
            foreach(var verbatimChecklist in verbatimChecklists)
            {
                var checklist = ArtportalenChecklistFactory.CreateProcessedChecklist(verbatimChecklist);
                checklists.Add(checklist);
            }

            return checklists;
        }

        public List<Observation> ProcessObservations(IEnumerable<DwcObservationVerbatim> verbatimObservations, bool initAreaHelper = false)
        {
            var processedObservations = new List<Observation>();
            bool diffuseIfSupported = false;            
            var factory = GetDarwinCoreFactory(initAreaHelper);
            foreach (var verbatimObservation in verbatimObservations)
            {
                var processedObservation = factory.CreateProcessedObservation(verbatimObservation, diffuseIfSupported);
                processedObservations.Add(processedObservation);
            }

            return processedObservations;
        }
    }
}