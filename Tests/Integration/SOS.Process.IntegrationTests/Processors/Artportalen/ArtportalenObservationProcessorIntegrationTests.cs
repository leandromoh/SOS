﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nest;
using SOS.Export.IO.DwcArchive;
using SOS.Export.Managers;
using SOS.Export.MongoDb;
using SOS.Export.Services;
using SOS.Lib.Configuration.Export;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Process.Database;
using SOS.Process.Helpers;
using SOS.Process.Processors.Artportalen;
using SOS.Process.Processors.DarwinCoreArchive;
using SOS.Process.Repositories.Destination;
using SOS.Process.Repositories.Destination.Interfaces;
using SOS.Process.Repositories.Source;
using Xunit;

namespace SOS.Process.IntegrationTests.Processors.Artportalen
{
    public class ArtportalenObservationProcessorIntegrationTests : TestBase
    {
        [Fact]
        public async Task Process_Artportalen_observations_with_CSV_writing()
        {
            // Current test
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var dwcArchiveFileWriterCoordinator = CreateDwcArchiveFileWriterCoordinator();
            var artportalenProcessor = CreateArtportalenObservationProcessor(dwcArchiveFileWriterCoordinator, storeProcessedObservations: false, 10000);
            var taxonByTaxonId = await GetTaxonDictionaryAsync();
            var dataProvider = new DataProvider
            {
                Id = 1,
                Identifier = "Artportalen",
                Name = "Artportalen",
                Type = DataProviderType.ArtportalenObservations
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            dwcArchiveFileWriterCoordinator.BeginWriteDwcCsvFiles();
            var processingStatus = await artportalenProcessor.ProcessAsync(dataProvider, taxonByTaxonId, JobCancellationToken.Null);
            await dwcArchiveFileWriterCoordinator.CreateDwcaFilesFromCreatedCsvFiles(); // FinishAndWriteDwcaFiles()
            dwcArchiveFileWriterCoordinator.DeleteTemporaryCreatedCsvFiles();

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            processingStatus.Status.Should().Be(RunStatus.Success);
        }

        private ArtportalenObservationProcessor CreateArtportalenObservationProcessor(
            DwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator,
            bool storeProcessedObservations,
            int batchSize)
        {
            var processConfiguration = GetProcessConfiguration();
            var exportConfiguration = GetExportConfiguration();
            var elasticConfiguration = GetElasticConfiguration();
            var uris = new Uri[elasticConfiguration.Hosts.Length];
            for (var i = 0; i < uris.Length; i++)
            {
                uris[i] = new Uri(elasticConfiguration.Hosts[i]);
            }

            var elasticClient = new ElasticClient(new ConnectionSettings(new StaticConnectionPool(uris)));
            var verbatimClient = new VerbatimClient(
                processConfiguration.VerbatimDbConfiguration.GetMongoDbSettings(),
                processConfiguration.VerbatimDbConfiguration.DatabaseName,
                processConfiguration.VerbatimDbConfiguration.BatchSize);
            var processClient = new ProcessClient(
                processConfiguration.ProcessedDbConfiguration.GetMongoDbSettings(),
                processConfiguration.ProcessedDbConfiguration.DatabaseName,
                processConfiguration.ProcessedDbConfiguration.BatchSize);
            var dwcaVerbatimRepository = new DwcaVerbatimRepository(
                verbatimClient,
                new NullLogger<DwcaVerbatimRepository>());
            var invalidObservationRepository =
                new InvalidObservationRepository(processClient, new NullLogger<InvalidObservationRepository>());
            IProcessedObservationRepository processedObservationRepository;
            if (storeProcessedObservations)
            {
                processedObservationRepository = new ProcessedObservationRepository(processClient, elasticClient,
                    invalidObservationRepository,
                    new ElasticSearchConfiguration(), new NullLogger<ProcessedObservationRepository>());
            }
            else
            {
                processedObservationRepository = CreateProcessedObservationRepositoryMock(batchSize).Object;
            }

            var processedFieldMappingRepository =
                new ProcessedFieldMappingRepository(processClient, new NullLogger<ProcessedFieldMappingRepository>());
            var artportalenVerbatimRepository = new ArtportalenVerbatimRepository(verbatimClient, new NullLogger<ArtportalenVerbatimRepository>());

            return new ArtportalenObservationProcessor(
                artportalenVerbatimRepository,
                processedObservationRepository,
                processedFieldMappingRepository,
                new FieldMappingResolverHelper(processedFieldMappingRepository, new FieldMappingConfiguration()),
                processConfiguration, 
                dwcArchiveFileWriterCoordinator,
                new NullLogger<ArtportalenObservationProcessor>());
        }

        private DwcArchiveFileWriterCoordinator CreateDwcArchiveFileWriterCoordinator()
        {
            var processConfiguration = GetProcessConfiguration();
            var exportClient = new ExportClient(
                processConfiguration.ProcessedDbConfiguration.GetMongoDbSettings(),
                processConfiguration.ProcessedDbConfiguration.DatabaseName,
                processConfiguration.ProcessedDbConfiguration.BatchSize);

            var dwcArchiveFileWriterCoordinator = new DwcArchiveFileWriterCoordinator(new DwcArchiveFileWriter(
                new DwcArchiveOccurrenceCsvWriter(
                    new Export.Repositories.ProcessedFieldMappingRepository(exportClient,
                        new NullLogger<Export.Repositories.ProcessedFieldMappingRepository>()),
                    new TaxonManager(
                        new Export.Repositories.ProcessedTaxonRepository(exportClient,
                            new NullLogger<Export.Repositories.ProcessedTaxonRepository>()),
                        new NullLogger<TaxonManager>()), new NullLogger<DwcArchiveOccurrenceCsvWriter>()),
                new ExtendedMeasurementOrFactCsvWriter(new NullLogger<ExtendedMeasurementOrFactCsvWriter>()),
                new FileService(),
                new NullLogger<DwcArchiveFileWriter>()
            ), new FileService(), new DwcaFilesCreationConfiguration { IsEnabled = true, FolderPath = @"c:\temp" }, new NullLogger<DwcArchiveFileWriterCoordinator>());
            return dwcArchiveFileWriterCoordinator;
        }

        private Mock<IProcessedObservationRepository> CreateProcessedObservationRepositoryMock(int batchSize)
        {
            var mock = new Mock<IProcessedObservationRepository>();
            mock.Setup(m => m.DeleteProviderDataAsync(It.IsAny<DataProvider>())).ReturnsAsync(true);
            mock.Setup(m => m.BatchSize).Returns(batchSize);
            return mock;
        }

        private async Task<IDictionary<int, ProcessedTaxon>> GetTaxonDictionaryAsync()
        {
            var processedTaxonRepository = CreateProcessedTaxonRepository();
            var taxa = await processedTaxonRepository.GetAllAsync();
            return taxa.ToDictionary(taxon => taxon.Id, taxon => taxon);
        }

        private ProcessedTaxonRepository CreateProcessedTaxonRepository()
        {
            var processConfiguration = GetProcessConfiguration();
            var processClient = new ProcessClient(
                processConfiguration.ProcessedDbConfiguration.GetMongoDbSettings(),
                processConfiguration.ProcessedDbConfiguration.DatabaseName,
                processConfiguration.ProcessedDbConfiguration.BatchSize);
            return new ProcessedTaxonRepository(
                processClient,
                new NullLogger<ProcessedTaxonRepository>());
        }
    }
}