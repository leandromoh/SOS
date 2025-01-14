﻿using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SOS.Export.IntegrationTests.TestHelpers.Factories;
using SOS.Lib.Cache;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Database;
using SOS.Lib.Helpers;
using SOS.Lib.IO.DwcArchive;
using SOS.Lib.Managers;
using SOS.Lib.Models.Processed.Configuration;
using SOS.Lib.Models.Search;
using SOS.Lib.Repositories.Processed;
using SOS.Lib.Repositories.Resource;
using SOS.Lib.Models.Shared;
using SOS.Lib.Services;
using Xunit;

namespace SOS.Export.IntegrationTests.IO.DwcArchive
{
    public class DwcArchiveFileWriterIntegrationTests : TestBase
    {
        private ProcessClient CreateExportClient(MongoDbConfiguration exportConfiguration)
        {
            var exportClient = new ProcessClient(
                exportConfiguration.GetMongoDbSettings(),
                exportConfiguration.DatabaseName,
                exportConfiguration.ReadBatchSize,
                exportConfiguration.WriteBatchSize);
            return exportClient;
        }

        private DwcArchiveFileWriter CreateDwcArchiveFileWriter(ProcessClient exportClient)
        {
            var processDbConfiguration = GetProcessDbConfiguration();
            var processClient = CreateExportClient(processDbConfiguration);
            var dwcArchiveFileWriter = new DwcArchiveFileWriter(
                new DwcArchiveOccurrenceCsvWriter(
                    CreateVocabularyValueResolver(CreateExportClient(processDbConfiguration)),
                    new NullLogger<DwcArchiveOccurrenceCsvWriter>()),
                new ExtendedMeasurementOrFactCsvWriter(new Mock<ILogger<ExtendedMeasurementOrFactCsvWriter>>().Object),
                new SimpleMultimediaCsvWriter(new NullLogger<SimpleMultimediaCsvWriter>()),
                new FileService(),
                new DataProviderRepository(processClient, new NullLogger<DataProviderRepository>()),
                new Mock<ILogger<DwcArchiveFileWriter>>().Object);
            return dwcArchiveFileWriter;
        }

        private VocabularyValueResolver CreateVocabularyValueResolver(ProcessClient client)
        {
            var vocabularyRepository =
                new VocabularyRepository(client, new NullLogger<VocabularyRepository>());
            return new VocabularyValueResolver(vocabularyRepository,
                new VocabularyConfiguration { LocalizationCultureCode = "sv-SE", ResolveValues = true });
        }

        private static ProcessedObservationRepository CreateProcessedObservationRepository(ProcessClient processClient, ElasticSearchConfiguration elasticConfiguration)
        {
            var processedObservationRepository = new ProcessedObservationRepository(
                new ElasticClientManager(elasticConfiguration, true),
                processClient,
                elasticConfiguration,
                new ClassCache<ProcessedConfiguration>(new MemoryCache(new MemoryDistributedCacheOptions())),
                new TelemetryClient(),
                new HttpContextAccessor(),
                new Mock<ILogger<ProcessedObservationRepository>>().Object);
            return processedObservationRepository;

        }

        [Fact(Skip = "Not working")]
        [Trait("Category", "Integration")]
        [Trait("Category", "DwcArchiveIntegration")]
        public async Task
            Creates_a_DwcArchiveFile_with_all_processed_data_in_MongoDb_instance_and_saves_the_file_on_disk()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var exportConfiguration = GetExportConfiguration();
            var exportFolderPath = exportConfiguration.FileDestination.Path;
            var processDbConfiguration = GetProcessDbConfiguration();
            var exportClient = CreateExportClient(processDbConfiguration);
            var elasticConfiguration = GetElasticConfiguration();
            var processedObservationRepository = CreateProcessedObservationRepository(exportClient, elasticConfiguration);
            var dwcArchiveFileWriter = CreateDwcArchiveFileWriter(exportClient);
            var processInfoRepository =
                new ProcessInfoRepository(exportClient, elasticConfiguration, new Mock<ILogger<ProcessInfoRepository>>().Object);
            var processInfo = await processInfoRepository.GetAsync(processInfoRepository.ActiveInstanceName);
            var filename = FilenameHelper.CreateFilenameWithDate("sos_dwc_archive_with_all_data");
            //var filter = new AdvancedFilter();
            var filter = new SearchFilter { Taxa = new TaxonFilter{ Ids = new[] { 102951 } } };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var zipFilePath = await dwcArchiveFileWriter.CreateDwcArchiveFileAsync(
                DataProvider.FilterSubsetDataProvider,
                filter,
                filename,
                processedObservationRepository,
                processInfo,
                exportFolderPath,
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            var fileExists = File.Exists(zipFilePath);
            fileExists.Should().BeTrue("because the zip file should have been generated");
        }

        [Fact(Skip = "Not working")]
        [Trait("Category", "Integration")]
        [Trait("Category", "DwcArchiveIntegration")]
        public async Task Creates_a_DwcArchiveFile_with_ten_predefined_observations_and_saves_the_file_on_disk()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var processDbConfiguration = GetProcessDbConfiguration();
            var exportClient = CreateExportClient(processDbConfiguration);
            var exportConfiguration = GetExportConfiguration();
            var elasticConfiguration = GetElasticConfiguration();
            var exportFolderPath = exportConfiguration.FileDestination.Path;
            var processedDarwinCoreRepositoryStub =
                ProcessedDarwinCoreRepositoryStubFactory.Create(@"Resources\TenProcessedTestObservations.json");
            var dwcArchiveFileWriter = CreateDwcArchiveFileWriter(exportClient);
            var processInfoRepository =
                new ProcessInfoRepository(exportClient, elasticConfiguration, new Mock<ILogger<ProcessInfoRepository>>().Object);
            var processInfo = await processInfoRepository.GetAsync(processInfoRepository.ActiveInstanceName);
            var filename = FilenameHelper.CreateFilenameWithDate("sos_dwc_archive_with_ten_observations");

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var zipFilePath = await dwcArchiveFileWriter.CreateDwcArchiveFileAsync(
                DataProvider.FilterSubsetDataProvider,
                new SearchFilter(),
                filename,
                processedDarwinCoreRepositoryStub.Object,
                processInfo,
                exportFolderPath,
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            var fileExists = File.Exists(zipFilePath);
            fileExists.Should().BeTrue("because the zip file should have been generated");
        }
    }
}