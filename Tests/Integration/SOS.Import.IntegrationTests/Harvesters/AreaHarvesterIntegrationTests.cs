﻿using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Import.Harvesters;
using SOS.Import.Repositories.Destination.Area;
using SOS.Import.Repositories.Source.Artportalen;
using SOS.Import.Services;
using SOS.Lib.Database;
using SOS.Lib.Enums;
using SOS.Lib.Repositories.Processed;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Process.Helpers;
using Xunit;

namespace SOS.Import.IntegrationTests.Harvesters
{
    public class AreaHarvesterIntegrationTests : TestBase
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task HarvestAllAreas_And_SaveToMongoDb()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var importConfiguration = GetImportConfiguration();
            var artportalenDataService = new ArtportalenDataService(importConfiguration.ArtportalenConfiguration);

            var verbatimDbConfiguration = GetVerbatimDbConfiguration();
            var areaVerbatimRepository = new AreaProcessedRepository(
                new ProcessClient(
                    verbatimDbConfiguration.GetMongoDbSettings(),
                    verbatimDbConfiguration.DatabaseName,
                    verbatimDbConfiguration.ReadBatchSize,
                    verbatimDbConfiguration.WriteBatchSize),
                new Mock<ILogger<AreaProcessedRepository>>().Object);

            var areaHarvester = new AreaHarvester(
                new AreaRepository(artportalenDataService, new Mock<ILogger<AreaRepository>>().Object),
                areaVerbatimRepository,
                new AreaHelper(new Mock<IProcessedAreaRepository>().Object, new Mock<IProcessedFieldMappingRepository>().Object), 
                new Mock<ILogger<AreaHarvester>>().Object);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await areaHarvester.HarvestAreasAsync();

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Status.Should().Be(RunStatus.Success);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task HarvestAllAreas_WithoutSavingToMongoDb()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var importConfiguration = GetImportConfiguration();
            var artportalenDataService = new ArtportalenDataService(importConfiguration.ArtportalenConfiguration);
            var verbatimDbConfiguration = GetVerbatimDbConfiguration();
            var areaVerbatimRepository = new AreaProcessedRepository(
                new ProcessClient(
                    verbatimDbConfiguration.GetMongoDbSettings(),
                    verbatimDbConfiguration.DatabaseName,
                    verbatimDbConfiguration.ReadBatchSize,
                    verbatimDbConfiguration.WriteBatchSize),
                new Mock<ILogger<AreaProcessedRepository>>().Object);

            var areaHarvester = new AreaHarvester(
                new AreaRepository(artportalenDataService, new Mock<ILogger<AreaRepository>>().Object),
                areaVerbatimRepository,
                new AreaHelper(new Mock<IProcessedAreaRepository>().Object, new Mock<IProcessedFieldMappingRepository>().Object),
                new Mock<ILogger<AreaHarvester>>().Object);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await areaHarvester.HarvestAreasAsync();

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Status.Should().Be(RunStatus.Success);
        }
    }
}