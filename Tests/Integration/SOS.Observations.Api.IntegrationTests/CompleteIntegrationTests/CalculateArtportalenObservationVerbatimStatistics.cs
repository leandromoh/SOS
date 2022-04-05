﻿using LinqStatistics;
using SOS.Lib.JsonConverters;
using SOS.Lib.Models.Verbatim.Artportalen;
using SOS.Observations.Api.IntegrationTests.Fixtures;
using SOS.TestHelpers.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SOS.Observations.Api.IntegrationTests.CompleteIntegrationTests
{
    [Collection(Collections.CompleteApiIntegrationTestsCollection)]
    public class CalculateArtportalenObservationVerbatimStatistics
    {
        private readonly CompleteApiIntegrationTestFixture _fixture;

        public CalculateArtportalenObservationVerbatimStatistics(CompleteApiIntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]        
        public async Task CalculateArtportalenVerbatimDataStatistics()
        {
            // Read observations from MongoDB
            using var cursor = await _fixture.ArtportalenVerbatimRepository.GetAllByCursorAsync();
            const int NrRowsToRead = 1000;
            int nrRowsRead = 0;
            var verbatimObservations = new List<ArtportalenObservationVerbatim>();
            while (await cursor.MoveNextAsync())
            {
                if (nrRowsRead >= NrRowsToRead) break;
                foreach (var row in cursor.Current)
                {
                    if (nrRowsRead >= NrRowsToRead) break;
                    verbatimObservations.Add(row);
                    nrRowsRead++;
                }
            }

            CalculateStatistics(verbatimObservations);            
        }

        private void CalculateStatistics(List<ArtportalenObservationVerbatim> observations)
        {           
            var activityStatistics = observations.Select(m => m.Activity?.Id)
                .CountEach()
                .OrderByDescending(m => m.Count)
                .ToList();

            var collectionIdStatistics = observations.Select(m => m.CollectionID)
                .CountEach()
                .OrderByDescending(m => m.Count)
                .ToList();

            // todo - add more statistics and write it to file            
        }
    }
}