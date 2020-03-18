﻿using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Import.Factories;
using SOS.Import.MongoDb;
using SOS.Import.ObservationHarvesters;
using SOS.Import.Repositories.Destination.Kul;
using SOS.Import.Repositories.Destination.Kul.Interfaces;
using SOS.Import.Services;
using SOS.Lib.Configuration.Import;
using SOS.Lib.Enums;
using Xunit;

namespace SOS.Import.IntegrationTests.Factories
{
    public class KulObservationFactoryIntegrationTests : TestBase
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task HarvestTenThousandObservations_FromKulProvider_And_SaveToMongoDb()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            ImportConfiguration importConfiguration = GetImportConfiguration();
            importConfiguration.KulServiceConfiguration.StartHarvestYear = 2015;
            importConfiguration.KulServiceConfiguration.MaxNumberOfSightingsHarvested = 10000;

            var kulObservationService = new KulObservationService(
                new Mock<ILogger<KulObservationService>>().Object, 
                importConfiguration.KulServiceConfiguration);
            
            var kulObservationVerbatimRepository = new KulObservationVerbatimRepository(
                new ImportClient(
                    importConfiguration.VerbatimDbConfiguration.GetMongoDbSettings(),
                    importConfiguration.VerbatimDbConfiguration.DatabaseName,
                    importConfiguration.VerbatimDbConfiguration.BatchSize), 
                new Mock<ILogger<KulObservationVerbatimRepository>>().Object);

        var kulObservationFactory = new KulObservationHarvester(
                kulObservationService,
                kulObservationVerbatimRepository, 
                importConfiguration.KulServiceConfiguration,
                new Mock<ILogger<KulObservationHarvester>>().Object);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await kulObservationFactory.HarvestObservationsAsync(JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Status.Should().Be(RunStatus.Success);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task HarvestTenThousandObservations_FromKulProvider_WithoutSavingToMongoDb()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            ImportConfiguration importConfiguration = GetImportConfiguration();
            importConfiguration.KulServiceConfiguration.StartHarvestYear = 2015;
            importConfiguration.KulServiceConfiguration.MaxNumberOfSightingsHarvested = 10000;
           
            var kulObservationFactory = new KulObservationHarvester(
                new KulObservationService(
                    new Mock<ILogger<KulObservationService>>().Object,
                    importConfiguration.KulServiceConfiguration),
                new Mock<IKulObservationVerbatimRepository>().Object,
                importConfiguration.KulServiceConfiguration,
                new Mock<ILogger<KulObservationHarvester>>().Object);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await kulObservationFactory.HarvestObservationsAsync(JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Status.Should().Be(RunStatus.Success);
        }
    }
}