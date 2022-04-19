﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson.Serialization.Conventions;
using Moq;
using SOS.Harvest.Managers;
using SOS.Harvest.Processors.Artportalen;
using SOS.Harvest.Processors.DarwinCoreArchive;
using SOS.Lib.Cache;
using SOS.Lib.Configuration.ObservationApi;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Database;
using SOS.Lib.Database.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Helpers;
using SOS.Lib.IO.DwcArchive;
using SOS.Lib.IO.Excel;
using SOS.Lib.IO.GeoJson;
using SOS.Lib.Managers;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Interfaces;
using SOS.Lib.Models.Processed.Configuration;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search;
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
using SOS.Observations.Api.Controllers;
using SOS.Observations.Api.HealthChecks;
using SOS.Observations.Api.Managers;
using SOS.Observations.Api.Managers.Interfaces;
using SOS.TestHelpers;
using DataProviderManager = SOS.Observations.Api.Managers.DataProviderManager;

namespace SOS.AutomaticIntegrationTests.TestFixtures
{
    public class IntegrationTestFixture : FixtureBase, IDisposable
    {
        public ArtportalenObservationProcessor ArtportalenObservationProcessor { get; set; }
        public ArtportalenObservationFactory ArtportalenObservationFactory { get; set; }
        public InstallationEnvironment InstallationEnvironment { get; private set; }
        public ObservationsController ObservationsController { get; private set; }
        public ExportsController ExportsController { get; private set; }
        public SystemsController SystemsController { get; private set; }
        public VocabulariesController VocabulariesController { get; private set; }
        public UserController UserController { get; private set; }
        public DataProvidersController DataProvidersController { get; private set; }
        public IProcessedObservationRepository ProcessedObservationRepository { get; set; }
        public IProcessedObservationRepository CustomProcessedObservationRepository { get; set; }
        public ObservationsController CustomObservationsController { get; private set; }
        public ArtportalenVerbatimRepository ArtportalenVerbatimRepository { get; set; }
        private DwcaObservationFactory _darwinCoreFactory;        

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
                _processTimeManager);

            return factory;                
        }

        public DwcArchiveFileWriter DwcArchiveFileWriter { get; set; }
        private IFilterManager _filterManager;
        private IUserManager _userManager;
        private VerbatimClient _importClient;
        private List<Taxon> _taxa;
        private Dictionary<int, Taxon> _taxaById;
        private VocabularyRepository _vocabularyRepository;
        private ProcessClient _processClient;
        private AreaHelper _areaHelper;
        private ProcessTimeManager _processTimeManager;
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
            CreateIntegrationTestIndexAsync().Wait();
        }

        public void Dispose()
        {
            // Delete integration test database
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
            var blobStorageManagerMock = new Mock<IBlobStorageManager>();
            var observationApiConfiguration = GetObservationApiConfiguration();
            var elasticClientManager = new ElasticClientManager(elasticConfiguration, true);
            var mongoDbConfiguration = GetMongoDbConfiguration();
            var processedSettings = mongoDbConfiguration.GetMongoDbSettings();
            _processClient = new ProcessClient(processedSettings, mongoDbConfiguration.DatabaseName,
                mongoDbConfiguration.ReadBatchSize, mongoDbConfiguration.WriteBatchSize);
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var areaManager = CreateAreaManager(_processClient);
            var taxonRepository = new TaxonRepository(_processClient, new NullLogger<TaxonRepository>());
            var taxonManager = CreateTaxonManager(_processClient, taxonRepository, memoryCache);
            var processedObservationRepository = CreateProcessedObservationRepository(elasticConfiguration, elasticClientManager, _processClient, memoryCache, taxonManager);
            _vocabularyRepository = new VocabularyRepository(_processClient, new NullLogger<VocabularyRepository>());
            var vocabularyManger = CreateVocabularyManager(_processClient, _vocabularyRepository);

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
            var userService = CreateUserService();
            var filterManager = new FilterManager(taxonManager, userService, areaCache, dataProviderCache);
            _filterManager = filterManager;
            var observationManager = CreateObservationManager(processedObservationRepository, _vocabularyValueResolver, _processClient, filterManager);

            var exportManager = new ExportManager(csvFileWriter, dwcArchiveFileWriter, excelFileWriter, geojsonFileWriter,
                processedObservationRepository, processInfoRepository, filterManager, new NullLogger<ExportManager>());
            var userExportRepository = new UserExportRepository(_processClient, new NullLogger<UserExportRepository>());
            ObservationsController = new ObservationsController(observationManager, taxonManager, areaManager, observationApiConfiguration, elasticConfiguration, new NullLogger<ObservationsController>());
            VocabulariesController = new VocabulariesController(vocabularyManger, new NullLogger<VocabulariesController>());
            DataProvidersController = new DataProvidersController(dataproviderManager, observationManager, new NullLogger<DataProvidersController>());
            ExportsController = new ExportsController(observationManager, blobStorageManagerMock.Object, areaManager,
                taxonManager, exportManager, fileService, userExportRepository, observationApiConfiguration,
                new NullLogger<ExportsController>());
            ExportsController.ControllerContext.HttpContext = new DefaultHttpContext();
            TaxonManager = taxonManager;
            ProcessedObservationRepository = processedObservationRepository;
            ElasticSearchConfiguration customElasticConfiguration = GetCustomSearchDbConfiguration();
            CustomProcessedObservationRepository = CreateProcessedObservationRepository(customElasticConfiguration, elasticClientManager, _processClient, memoryCache, taxonManager);
            var customObservationManager = CreateObservationManager((ProcessedObservationRepository)CustomProcessedObservationRepository, _vocabularyValueResolver, _processClient, filterManager);
            CustomObservationsController = new ObservationsController(customObservationManager, taxonManager, areaManager, observationApiConfiguration, customElasticConfiguration, new NullLogger<ObservationsController>());
            DwcArchiveFileWriter = dwcArchiveFileWriter;
            var healthCheckConfiguration = new HealthCheckConfiguration
            {
                AzureApiUrl = GetAzureApiUrl(),
                AzureSubscriptionKey = GetAzureApiSubscriptionKey()
            };
            SearchDataProvidersHealthCheck = new SearchDataProvidersHealthCheck(observationManager, dataProviderCache);
            SearchPerformanceHealthCheck = new SearchPerformanceHealthCheck(observationManager);
            AzureSearchHealthCheck = new AzureSearchHealthCheck(healthCheckConfiguration);
            SystemsController = new SystemsController(processInfoManager, processedObservationRepository, new NullLogger<SystemsController>());
            _userManager = new UserManager(userService, new NullLogger<UserManager>());
            UserController = new UserController(_userManager, new NullLogger<UserController>());
            var artportalenDataProvider = new Lib.Models.Shared.DataProvider { Id = 1 };
            _taxa = await taxonRepository.GetAllAsync();
            _taxaById = _taxa.ToDictionary(m => m.Id, m => m);
            _processTimeManager = new ProcessTimeManager(new ProcessConfiguration());            
            ArtportalenObservationFactory = await ArtportalenObservationFactory.CreateAsync(
                artportalenDataProvider,
                _taxaById,
                _vocabularyRepository,
                false,
                "https:\\www.artportalen.se",
                _processTimeManager);
            var verbatimDbConfiguration = GetVerbatimMongoDbConfiguration();
            _importClient = new VerbatimClient(
                verbatimDbConfiguration.GetMongoDbSettings(),
                verbatimDbConfiguration.DatabaseName,
                verbatimDbConfiguration.ReadBatchSize,
                verbatimDbConfiguration.WriteBatchSize);
            ArtportalenVerbatimRepository = new ArtportalenVerbatimRepository(_importClient, new NullLogger<ArtportalenVerbatimRepository>());
            DwcArchiveOccurrenceCsvWriter = new DwcArchiveOccurrenceCsvWriter(_vocabularyValueResolver, new NullLogger<DwcArchiveOccurrenceCsvWriter>());
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

        private TaxonManager CreateTaxonManager(ProcessClient processClient, TaxonRepository taxonRepository, IMemoryCache memoryCache)
        {
            var taxonListRepository = new TaxonListRepository(processClient, new NullLogger<TaxonListRepository>());
            var taxonManager = new TaxonManager(taxonRepository, taxonListRepository,
                new ClassCache<TaxonTree<IBasicTaxon>>(memoryCache),
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
            var artportalenApiService = new ArtportalenApiService(new Mock<IAuthorizationProvider>().Object,
                new HttpClientService(new NullLogger<HttpClientService>()),
                new ArtportalenApiServiceConfiguration { BaseAddress = "https://api.artdata.slu.se/observations/v2", AcceptHeaderContentType = "application/json" },
                new NullLogger<ArtportalenApiService>());
            var artportalenApiManager = new ArtportalenApiManager(artportalenApiService, new NullLogger<ArtportalenApiManager>());


            var observationsManager = new ObservationManager(processedObservationRepository,
                protectedLogRepository,
                vocabularyValueResolver,
                filterManager,
                new HttpContextAccessor(),
                new TaxonObservationCountCache(),
                artportalenApiManager,
                new ClassCache<Dictionary<int, TaxonSumAggregationItem>>(new MemoryCache(new MemoryCacheOptions())) { CacheDuration = TimeSpan.FromHours(4) },
                new NullLogger<ObservationManager>());

            return observationsManager;
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
            var vocabularyManager = new VocabularyManager(vocabularyCache, projectInfoCache, new NullLogger<VocabularyManager>());
            return vocabularyManager;
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
                processClient,
                elasticConfiguration,
                new ProcessedConfigurationCache(new ProcessedConfigurationRepository(processClient, new NullLogger<ProcessedConfigurationRepository>())),
                new TelemetryClient(),
                new HttpContextAccessor(),
                taxonManager,
                new NullLogger<ProcessedObservationRepository>());
            return processedObservationRepository;
        }

        public void UseMockUserService(params AuthorityModel[] authorities)
        {
            UserModel user = new UserModel();
            user.Id = 15;
            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(userService => userService.GetUserAsync())
                .ReturnsAsync(user);
            userServiceMock.Setup(userService =>
                    userService.GetUserAuthoritiesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authorities);
            _filterManager.UserService = userServiceMock.Object;

            var contextAccessor = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var claimsIdentity = new ClaimsIdentity();
            var claim = new Claim("scope", "SOS.Observations.Protected");
            claimsIdentity.AddClaim(claim);
            contextAccessor.HttpContext.User.AddIdentity(claimsIdentity);
            ProcessedObservationRepository.HttpContextAccessor = contextAccessor;
        }


        public void UseUserServiceWithToken(string token)
        {
            var userService = CreateUserService(token);
            _filterManager.UserService = userService;
            _userManager.UserService = userService;
        }

        private async Task CreateIntegrationTestIndexAsync()
        {
            const bool protectedIndex = false;
            await CustomProcessedObservationRepository.ClearCollectionAsync(protectedIndex);
        }

        public async Task AddObservationsToElasticsearchAsync(IEnumerable<Observation> observations, bool clearExistingObservations = true)
        {
            const bool protectedIndex = false;
            if (clearExistingObservations)
            {
                await CustomProcessedObservationRepository.DeleteAllDocumentsAsync(protectedIndex);
            }
            await CustomProcessedObservationRepository.DisableIndexingAsync(protectedIndex);
            await CustomProcessedObservationRepository.AddManyAsync(observations, protectedIndex);
            await CustomProcessedObservationRepository.EnableIndexingAsync(protectedIndex);
            Thread.Sleep(1000);
        }

        public async Task ProcessAndAddObservationsToElasticSearch(IEnumerable<ArtportalenObservationVerbatim> verbatimObservations)
        {
            var processedObservations = ProcessObservations(verbatimObservations);            
            await AddObservationsToElasticsearchAsync(processedObservations);
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