﻿using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using SOS.Administration.Gui.Dtos;
using SOS.Administration.Gui.Dtos.Enum;

namespace SOS.Administration.Gui.Services
{

    public class Test
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public string Route { get; set; }        

        public delegate Task<TestResults> RunTestFunction();
        [System.Text.Json.Serialization.JsonIgnore]
        public RunTestFunction RunTest { get; set; }
    }
    public class TestResults
    {
        public int TestId { get; set; }
        public long TimeTakenMs { get; set; }
        public IEnumerable<TestResult> Results { get; set; }
    }
    public class TestResult
    {
        public string Result { get; set; }
        public string Status { get; set; }
    }
    public class ObservationTaxon
    {
        public int Id { get; set; }
        public string VernacularName { get; set; }
    }
    public class ObservationLocation
    {
        public int Id { get; set; }
        public ObservationPlace Municipality { get; set; }
        public double DecimalLatitude { get; set; }
        public double DecimalLongitude { get; set; }
        public double CoordinateUncertaintyInMeters { get; set; }
    }
    public class ObservationPlace
    {
        public string FeatureId { get; set; }
        public string Name { get; set; }
    }
    public class ObservationOccurrence
    {
        public string OccurrenceId { get; set; }
    }
    public class SOSObservation
    {
        public string DataSetId { get; set; }
        public string DataSetName { get; set; }
        public ObservationOccurrence Occurrence { get; set; }
        public ObservationTaxon Taxon { get; set; }
        public ObservationLocation Location { get; set; }
    }
    public class TestService : ITestService
    {

        private readonly HttpClient _client;
        private readonly string _apiUrl;
        private readonly List<Test> _tests;
        private readonly ISearchService _searchService;
        public TestService(IOptionsMonitor<ApiTestConfiguration> optionsMonitor, ISearchService searchService)
        {
            _client = new HttpClient();
            _apiUrl = optionsMonitor.CurrentValue.ApiUrl;
            _searchService = searchService;
            _tests = new List<Test>() {
                new Test()
                {
                    Id = 0,
                    Description = "Search for Otters",
                    Group = "Search",
                    Route = "test_searchotter",
                    RunTest = Test_SearchOtter
                },
                 new Test()
                {
                    Id = 1,
                    Description = "Search for Otters in Tranås",
                    Group = "Search",
                    Route = "test_searchotteratlocation",
                    RunTest = Test_SearchOtterAtLocation
                },
                new Test()
                {
                    Id = 2,
                    Description = "Search for Wolfs",
                    Group = "Search",
                    Route = "test_searchwolf",
                    RunTest = Test_SearchWolf
                },
                new Test()
                {
                    Id = 3,
                    Description = "Get DataProviders",
                    Group = "DataProviders",
                    Route = "test_dataproviders",
                    RunTest = Test_DataProviders
                }
                ,
                new Test()
                {
                    Id = 4,
                    Description = "Get Vocabulary",
                    Group = "Vocabularies",
                    Route = "test_vocabularies",
                    RunTest = Test_Vocabulary
                },
                new Test()
                {
                    Id = 5,
                    Description = "GeoAggregation of all mammals",
                    Group = "Aggregations",
                    Route = "test_geogridaggregation",
                    RunTest = Test_GeoGridAggregation
                },
                new Test()
                {
                    Id = 6,
                    Description = "TaxonAggregation of all taxon",
                    Group = "Aggregations",
                    Route = "test_taxonaggregation",
                    RunTest = Test_TaxonAggregation
                },
                 new Test()
                {
                    Id = 7,
                    Description = "TaxonAggregation of all taxon with boundingbox",
                    Group = "Aggregations",
                    Route = "test_taxonaggregationbbox",
                    RunTest = Test_TaxonAggregationBBox
                }
            };

        }
        public IEnumerable<Test> GetTests()
        {
            return _tests;
        }
        public async Task<TestResults> Test_SearchOtter()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 0;
            SearchFilterDto searchFilter = new SearchFilterDto();
            searchFilter.Taxon = new TaxonFilterDto()
            {
                Ids = new List<int>() { 100077 },
                IncludeUnderlyingTaxa = true
            };
            searchFilter.Date = new DateFilterDto()
            {
                StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                EndDate = new DateTime(2020, 1, 31, 07, 59, 46)
            };
            searchFilter.ValidationStatus = SearchFilterBaseDto.StatusValidationDto.BothValidatedAndNotValidated;
            searchFilter.OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present;
            PagedResult<SOSObservation> result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                result = await _searchService.SearchSOS(searchFilter, 2, 0);
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.Equal(2, result.Records.Count()); results.Add(new TestResult() { Result = "Returns Two records", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns two records:" + e.Message, Status = "Failed" }); }

            try { Assert.True(result.TotalCount > 5000); results.Add(new TestResult() { Result = "More than 5000 totalCount ", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "More than 5000 totalCount records:" + e.Message, Status = "Failed" }); }

            try { Assert.Equal(100077, result.Records.First().Taxon.Id); results.Add(new TestResult() { Result = "TaxonId equals 100077", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "TaxonId equals 100077:" + e.Message, Status = "Failed" }); }

            return testResults;
        }

        public async Task<TestResults> Test_SearchOtterAtLocation()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 0;
            SearchFilterDto searchFilter = new SearchFilterDto();
            searchFilter.Taxon = new TaxonFilterDto()
            {
                Ids = new List<int>() { 100077 },
                IncludeUnderlyingTaxa = true
            };
            searchFilter.Geographics = new GeographicsFilterDto{Areas = new[] { new AreaFilterDto { AreaType = AreaTypeDto.Municipality, FeatureId = "687" } } };
            searchFilter.Date = new DateFilterDto()
            {
                StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                EndDate = new DateTime(2020, 1, 31, 07, 59, 46)
            };
            searchFilter.ValidationStatus = SearchFilterBaseDto.StatusValidationDto.BothValidatedAndNotValidated;
            searchFilter.OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present;
            PagedResult<SOSObservation> result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                result = await _searchService.SearchSOS(searchFilter, 2, 0);
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.Equal("687", result.Records.First().Location.Municipality.FeatureId); results.Add(new TestResult() { Result = "Location Municipality FeatureId equals 687", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Location Municipality FeatureId equals 687:" + e.Message, Status = "Failed" }); }

            try { Assert.Equal(100077, result.Records.First().Taxon.Id); results.Add(new TestResult() { Result = "Taxon id equals 100077", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Taxon id equals 100077:" + e.Message, Status = "Failed" }); }

            return testResults;
        }
        public async Task<TestResults> Test_SearchWolf()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 1;
            SearchFilterDto searchFilter = new SearchFilterDto();
            searchFilter.Taxon = new TaxonFilterDto()
            {
                Ids = new List<int>() { 100024 },
                IncludeUnderlyingTaxa = true
            };
            searchFilter.OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present;

            PagedResult<SOSObservation> result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {

                result = await _searchService.SearchSOS(searchFilter, 10, 0);
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.Empty(result.Records); results.Add(new TestResult() { Result = "Returns 0 records", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns 0 records:" + e.Message, Status = "Failed" }); }

            return testResults;
        }

        public async Task<TestResults> Test_GeoGridAggregation()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 1;
            SearchFilterDto searchFilter = new SearchFilterDto();
            searchFilter.Taxon = new TaxonFilterDto()
            {
                Ids = new List<int>() { 4000107 },
                IncludeUnderlyingTaxa = true
            };
            searchFilter.Date = new DateFilterDto()
            {
                StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                EndDate = new DateTime(2020, 1, 31, 07, 59, 46)
            };
            searchFilter.ValidationStatus = SearchFilterBaseDto.StatusValidationDto.BothValidatedAndNotValidated;
            searchFilter.OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present;

            GeoGridResultDto result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {

                result = await _searchService.SearchSOSGeoAggregation(searchFilter);
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.True(result.GridCellCount > 1000); results.Add(new TestResult() { Result = "Returns >100 gridcells", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >100 gridcells:" + e.Message, Status = "Failed" }); }

            try { Assert.True(result.GridCells.First().ObservationsCount > 1000); results.Add(new TestResult() { Result = "Returns >1000 observations in first gridcell", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >1000 observations in first gridcell:" + e.Message, Status = "Failed" }); }

            return testResults;
        }
        public async Task<TestResults> Test_TaxonAggregation()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 1;
            SearchFilterDto searchFilter = new SearchFilterDto();
            searchFilter.Date = new DateFilterDto()
            {
                StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                EndDate = new DateTime(2020, 1, 31, 07, 59, 46)
            };
            searchFilter.ValidationStatus = SearchFilterBaseDto.StatusValidationDto.BothValidatedAndNotValidated;
            searchFilter.OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present;

            PagedResult<TaxonAggregationItemDto> result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {

                result = await _searchService.SearchSOSTaxonAggregation(searchFilter, 100, 0);
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.True(result.TotalCount > 30000); results.Add(new TestResult() { Result = "Returns >30000 results", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >30000 results:" + e.Message, Status = "Failed" }); }

            try { Assert.True(result.Records.First().ObservationCount > 100000); results.Add(new TestResult() { Result = "Returns >100 000 results from first taxon", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >100 000 results from first taxon" + e.Message, Status = "Failed" }); }

            return testResults;
        }
        public async Task<TestResults> Test_TaxonAggregationBBox()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 1;
            SearchFilterDto searchFilter = new SearchFilterDto();
            searchFilter.Date = new DateFilterDto()
            {
                StartDate = new DateTime(1990, 1, 31, 07, 59, 46),
                EndDate = new DateTime(2020, 1, 31, 07, 59, 46)
            };
            searchFilter.ValidationStatus = SearchFilterBaseDto.StatusValidationDto.BothValidatedAndNotValidated;
            searchFilter.OccurrenceStatus = OccurrenceStatusFilterValuesDto.Present;

            PagedResult<TaxonAggregationItemDto> result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {

                result = await _searchService.SearchSOSTaxonAggregation(searchFilter, 500, 0, 17.9296875, 59.355596110016315, 18.28125, 59.17592824927137);
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.True(result.TotalCount > 8000); results.Add(new TestResult() { Result = "Returns >8000 results", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >8000 results:" + e.Message, Status = "Failed" }); }

            try { Assert.True(result.Records.First().ObservationCount > 2500); results.Add(new TestResult() { Result = "Returns >2500 results from first taxon", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >2500 results from first taxon" + e.Message, Status = "Failed" }); }

            return testResults;
        }
        public async Task<TestResults> Test_DataProviders()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 1;
            IEnumerable<DataProvider> result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                var response = await _client.GetAsync($"{_apiUrl}DataProviders");
                if (response.IsSuccessStatusCode)
                {
                    var resultString = response.Content.ReadAsStringAsync().Result;
                    result = JsonConvert.DeserializeObject<IEnumerable<DataProvider>>(resultString);
                    results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
                }
                else
                {
                    throw new Exception("Call to API failed, responseCode:" + response.StatusCode);
                }
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.True(result.Count() > 10); results.Add(new TestResult() { Result = "Returns >10 records", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Returns >10 records:" + e.Message, Status = "Failed" }); }

            return testResults;
        }
        public async Task<TestResults> Test_Vocabulary()
        {
            TestResults testResults = new TestResults();
            var results = new List<TestResult>();
            testResults.Results = results;
            testResults.TestId = 1;
            Vocabulary result;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                var response = await _client.GetAsync($"{_apiUrl}Vocabularies/LifeStage");
                if (response.IsSuccessStatusCode)
                {
                    var resultString = response.Content.ReadAsStringAsync().Result;
                    result = JsonConvert.DeserializeObject<Vocabulary>(resultString);
                    results.Add(new TestResult() { Result = "Call api", Status = "Succeeded" });
                }
                else
                {
                    throw new Exception("Call to API failed, responseCode:" + response.StatusCode);
                }
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
            }
            catch (Exception e)
            {
                sw.Stop();
                testResults.TimeTakenMs = sw.ElapsedMilliseconds;
                results.Add(new TestResult() { Result = "Call api:" + e.Message, Status = "Failed" });
                return testResults;
            }

            try { Assert.Equal("LifeStage", result.Name); results.Add(new TestResult() { Result = "Name is LifeStage", Status = "Succeeded" }); }
            catch (Exception e) { results.Add(new TestResult() { Result = "Name is LifeStage:" + e.Message, Status = "Failed" }); }

            return testResults;
        }
    }
}
