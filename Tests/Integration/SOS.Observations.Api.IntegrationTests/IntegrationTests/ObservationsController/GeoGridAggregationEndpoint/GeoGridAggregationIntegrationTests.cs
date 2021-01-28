﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SOS.Lib.Models.Gis;
using SOS.Lib.Models.Search;
using SOS.Observations.Api.Dtos;
using SOS.Observations.Api.Dtos.Filter;
using SOS.Observations.Api.IntegrationTests.Extensions;
using SOS.Observations.Api.IntegrationTests.Fixtures;
using Xunit;

namespace SOS.Observations.Api.IntegrationTests.IntegrationTests.ObservationsController.GeoGridAggregationEndpoint
{
    [Collection(Collections.ApiIntegrationTestsCollection)]
    public class GeoGridAggregationIntegrationTests
    {
        private readonly ApiIntegrationTestFixture _fixture;

        public GeoGridAggregationIntegrationTests(ApiIntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("Category", "ApiIntegrationTest")]
        public async Task GeoGridAggregation_Mammalia()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var searchFilter = new SearchFilterAggregationDto
            {
                Taxon = new TaxonFilterDto { TaxonIds = new List<int>() { 4000107 }, IncludeUnderlyingTaxa = true },
                Date = new DateFilterDto
                {
                    StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                    EndDate = new DateTime(2020, 1, 31, 07, 59, 46)
                },
                OnlyValidated = false,
                OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var response = await _fixture.ObservationsController.GeogridSearchTileBasedAggregationAsync(searchFilter, 10);
            var result = response.GetResult<GeoGridResultDto>();

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.GridCellCount.Should().BeGreaterThan(1000);
            result.GridCells.First().ObservationsCount.Should().BeGreaterThan(1000);
        }

        [Fact]
        [Trait("Category", "ApiIntegrationTest")]
        public async Task PagedGeoTileTaxaAggregation_Mammalia()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var searchFilter = new SearchFilterAggregationInternalDto()
            {
                Taxon = new TaxonFilterDto { TaxonIds = new List<int>() { 4000107 }, IncludeUnderlyingTaxa = true },
                Date = new DateFilterDto
                {
                    StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                    EndDate = new DateTime(2021, 1, 31, 07, 59, 46)
                },
                OnlyValidated = false,
                OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present
            };
            int zoom = 10;
            var gridCells = new List<GeoGridTileTaxaCellDto>();
            int nrRequests = 0;
            var sp = Stopwatch.StartNew();

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var response = await _fixture.ObservationsController.InternalPagedGeogridTaxaAggregationAsync(searchFilter, zoom);
            var result = response.GetResult<GeoGridTileTaxonPageResultDto>();
            gridCells.AddRange(result.GridCells);
            nrRequests++;
            while (result.HasMorePages)
            {
                response = await _fixture.ObservationsController.InternalPagedGeogridTaxaAggregationAsync(
                    searchFilter, 
                    zoom, 
                    result.GeoTilePage, 
                    result.TaxonIdPage);
                result = response.GetResult<GeoGridTileTaxonPageResultDto>();
                gridCells.AddRange(result.GridCells);
                nrRequests++;
            }

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            sp.Stop();
            int observationCount = gridCells
                .Sum(m => m.Taxa
                    .Sum(v => v.ObservationCount));
            gridCells.Count.Should().BeGreaterThan(1000);
            observationCount.Should().BeGreaterThan(100000);
            nrRequests.Should().BeGreaterOrEqualTo(1);
        }

        //[Fact]
        //[Trait("Category", "ApiIntegrationTest")]
        //public async Task CompleteGeoTileTaxaAggregation_Mammalia()
        //{
        //    //-----------------------------------------------------------------------------------------------------------
        //    // Arrange
        //    //-----------------------------------------------------------------------------------------------------------
        //    var searchFilter = new SearchFilterAggregationInternalDto()
        //    {
        //        Taxon = new TaxonFilterDto { TaxonIds = new List<int>() { 4000107 }, IncludeUnderlyingTaxa = true },
        //        Date = new DateFilterDto
        //        {
        //            StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
        //            EndDate = new DateTime(2021, 1, 31, 07, 59, 46)
        //        },
        //        OnlyValidated = false,
        //        OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present
        //    };
        //    int zoom = 10;
        //    var sp = Stopwatch.StartNew();

        //    //-----------------------------------------------------------------------------------------------------------
        //    // Act
        //    //-----------------------------------------------------------------------------------------------------------
        //    var response = await _fixture.ObservationsController.InternalCompleteGeogridTaxaAggregationAsync(searchFilter, zoom);
        //    var result = (response.GetResult<IEnumerable<GeoGridTileTaxaCellDto>>()).ToList();

        //    //-----------------------------------------------------------------------------------------------------------
        //    // Assert
        //    //-----------------------------------------------------------------------------------------------------------
        //    sp.Stop();
        //    int observationCount = result
        //        .Sum(m => m.Taxa
        //            .Sum(v => v.ObservationCount));
        //    result.Count.Should().BeGreaterThan(1000);
        //    observationCount.Should().BeGreaterThan(100000);
        //}
    }
}