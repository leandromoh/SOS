﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using SOS.Lib.Cache;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Database;
using SOS.Lib.Managers;
using SOS.Lib.Models.Processed.Configuration;
using SOS.Lib.Models.Search;
using SOS.Lib.Repositories.Processed;
using SOS.TestHelpers.JsonConverters;
using Xunit;

namespace SOS.Export.IntegrationTests.TestDataTools
{
    public class CreateObservationsJsonFileTool : TestBase
    {
        /// <summary>
        ///     Reads observations from MongoDb and saves them as a JSON file.
        /// </summary>
        [Fact(Skip = "Not working")]
        [Trait("Category", "Tool")]
        public async Task CreateObservationsJsonFile()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            const string filePath = @"c:\temp\TenProcessedTestObservations.json";
            var elasticSearchConfiguration = new ElasticSearchConfiguration();
            var processDbConfiguration = GetProcessDbConfiguration();
            var exportClient = new ProcessClient(
                processDbConfiguration.GetMongoDbSettings(),
                processDbConfiguration.DatabaseName,
                processDbConfiguration.ReadBatchSize,
                processDbConfiguration.WriteBatchSize);
            var processedObservationRepository = new ProcessedObservationRepository(
                new ElasticClientManager(elasticSearchConfiguration, true),
                exportClient,
                new ElasticSearchConfiguration(),
                new ClassCache<ProcessedConfiguration>(new MemoryCache(new MemoryCacheOptions())),
                new Mock<ILogger<ProcessedObservationRepository>>().Object);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observations = await processedObservationRepository.ScrollObservationsAsync(new SearchFilter(), null);

            var serializerSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> {new ObjectIdConverter()}
            };
            var strJson = JsonConvert.SerializeObject(observations, serializerSettings);
            File.WriteAllText(filePath, strJson, Encoding.UTF8);
        }
    }
}