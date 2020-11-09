﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SOS.Lib.Enums;
using SOS.Lib.Models.Gis;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;
using SOS.Observations.Api.Controllers.Interfaces;
using SOS.Observations.Api.Dtos;
using SOS.Observations.Api.Dtos.Filter;
using SOS.Observations.Api.Extensions;
using SOS.Observations.Api.Managers.Interfaces;
using SOS.Observations.Api.Swagger;

namespace SOS.Observations.Api.Controllers
{
    /// <summary>
    ///     Observation controller
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class ObservationsController : ControllerBase, IObservationsController
    {
        private const int MaxBatchSize = 1000;
        private const int ElasticSearchMaxRecords = 10000;
        private readonly IVocabularyManager _vocabularyManager;
        private readonly ILogger<ObservationsController> _logger;
        private readonly IObservationManager _observationManager;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="observationManager"></param>
        /// <param name="vocabularyManager"></param>
        /// <param name="logger"></param>
        public ObservationsController(
            IObservationManager observationManager,
            IVocabularyManager vocabularyManager,
            ILogger<ObservationsController> logger)
        {
            _observationManager = observationManager ?? throw new ArgumentNullException(nameof(observationManager));
            _vocabularyManager = vocabularyManager ?? throw new ArgumentNullException(nameof(vocabularyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        [HttpPost("Search")]
        [ProducesResponseType(typeof(PagedResultDto<Observation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> SearchAsync(
            [FromBody] SearchFilterDto filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] string sortBy = "",
            [FromQuery] SearchSortOrder sortOrder = SearchSortOrder.Asc)
        {
            try
            {
                var pagingArgumentsValidation = ValidateSearchPagingArguments(skip, take);
                var searchFilterValidation = ValidateSearchFilter(filter);
                var validationResult = Result.Combine(pagingArgumentsValidation, searchFilterValidation);
                if (validationResult.IsFailure) return BadRequest(validationResult.Error);

                SearchFilter searchFilter = filter.ToSearchFilter();
                var result = await _observationManager.GetChunkAsync(searchFilter, skip, take, sortBy, sortOrder);
                PagedResultDto<dynamic> dto = result.ToPagedResultDto(result.Records);
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Search error");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc />
        [HttpPost("SearchInternal")]
        [ProducesResponseType(typeof(PagedResultDto<Observation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [InternalApi]
        public async Task<IActionResult> SearchInternalAsync(
            [FromBody] SearchFilterInternalDto filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] string sortBy = "",
            [FromQuery] SearchSortOrder sortOrder = SearchSortOrder.Asc)
        {
            try
            {
                var pagingArgumentsValidation = ValidateSearchPagingArgumentsInternal(skip, take);
                var searchFilterValidation = ValidateSearchFilterInternal(filter);
                var validationResult = Result.Combine(pagingArgumentsValidation, searchFilterValidation);
                if (validationResult.IsFailure) return BadRequest(validationResult.Error);

                var result = await _observationManager.GetChunkAsync(filter.ToSearchFilterInternal(), skip, take, sortBy, sortOrder);
                PagedResultDto<dynamic> dto = result.ToPagedResultDto(result.Records);
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SearchInternal error");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc />
        [HttpPost("SearchAggregatedInternal")]
        [ProducesResponseType(typeof(PagedResultDto<Observation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [InternalApi]
        public async Task<IActionResult> SearchAggregatedInternalAsync(
            [FromBody] SearchFilterInternalDto filter,
            [FromQuery] AggregationType aggregationType,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] string sortBy = "",
            [FromQuery] SearchSortOrder sortOrder = SearchSortOrder.Asc
            )
        {
            try
            {
                var filterValidation = ValidateSearchFilterInternal(filter);
                var pagingArgumentsValidation = ValidatePagingArguments(skip, take);
                var paramsValidationResult = Result.Combine(filterValidation, pagingArgumentsValidation);
                if (paramsValidationResult.IsFailure)
                {
                    return BadRequest(paramsValidationResult.Error);
                }

                var result = await _observationManager.GetAggregatedChunkAsync(filter.ToSearchFilterInternal(), aggregationType, skip, take, sortBy, sortOrder);
                PagedResultDto<dynamic> dto = result.ToPagedResultDto(result.Records);
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SearchAggregatedInternal error");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Aggregates observations into grid cells. Each grid cell contains the number
        /// of observations and the number of unique taxa (usually species) in the grid cell.
        /// The grid cells are squares in WGS84 coordinate system which means that they also
        /// will be squares in the WGS84 Web Mercator coordinate system.
        /// </summary>
        /// <remarks>
        /// The following table shows the approximate grid cell size (width) in different
        /// coordinate systems for the different zoom levels.
        /// | Zoom level | WGS84    | Web Mercator  |  SWEREF99TM(Southern Sweden) |  SWEREF99TM(North Sweden) |
        /// |------------|----------|---------------|:----------------------------:|:-------------------------:|
        /// | 1          |      180 |       20000km |                       8000km |                   12000km |
        /// | 2          |       90 |       10000km |                       4000km |                    6000km |
        /// | 3          |       45 |        5000km |                       2000km |                    3000km |
        /// | 4          |     22.5 |        2500km |                       1000km |                    1500km |
        /// | 5          |    11.25 |        1250km |                        500km |                     750km |
        /// | 6          |    5.625 |         600km |                        250km |                     360km |
        /// | 7          |   2.8125 |         300km |                        120km |                     180km |
        /// | 8          | 1.406250 |         150km |                         60km |                      90km |
        /// | 9          | 0.703125 |          80km |                         30km |                      45km |
        /// | 10         | 0.351563 |          40km |                         15km |                      23km |
        /// | 11         | 0.175781 |          20km |                          8km |                      11km |
        /// | 12         | 0.087891 |          10km |                          4km |                       6km |
        /// | 13         | 0.043945 |           5km |                          2km |                       3km |
        /// | 14         | 0.021973 |         2500m |                        1000m |                     1400m |
        /// | 15         | 0.010986 |         1200m |                         500m |                      700m |
        /// | 16         | 0.005493 |          600m |                         240m |                      350m |
        /// | 17         | 0.002747 |          300m |                         120m |                      180m |
        /// | 18         | 0.001373 |          150m |                          60m |                       90m |
        /// | 19         | 0.000687 |           80m |                          30m |                       45m |
        /// | 20         | 0.000343 |           40m |                          15m |                       22m |
        /// | 21         | 0.000172 |           19m |                           7m |                       11m |
        /// </remarks>
        /// <param name="filter">The search filter.</param>
        /// <param name="zoom">A zoom level between 1 and 21.</param>
        /// <param name="bboxLeft">Bounding box left (longitude) coordinate in WGS84.</param>
        /// <param name="bboxTop">Bounding box top (latitude) coordinate in WGS84.</param>
        /// <param name="bboxRight">Bounding box right (longitude) coordinate in WGS84.</param>
        /// <param name="bboxBottom">Bounding box bottom (latitude) coordinate in WGS84.</param>
        /// <returns></returns>
        [HttpPost("GeoGridAggregation")]
        [ProducesResponseType(typeof(GeoGridResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GeogridSearchTileBasedAggregationAsync(
            [FromBody] SearchFilterDto filter,
            [FromQuery] int zoom = 1,
            [FromQuery] double? bboxLeft = null,
            [FromQuery] double? bboxTop = null,
            [FromQuery] double? bboxRight = null,
            [FromQuery] double? bboxBottom = null)
        {
            try
            {
                var filterValidation = ValidateSearchFilter(filter);
                var zoomOrError = ValidateGeogridZoomArgument(zoom, minLimit: 1, maxLimit: 21);
                var bboxOrError = LatLonBoundingBox.Create(bboxLeft, bboxTop, bboxRight, bboxBottom);
                var paramsValidationResult = Result.Combine(filterValidation, zoomOrError, bboxOrError);
                if (paramsValidationResult.IsFailure)
                {
                    return BadRequest(paramsValidationResult.Error);
                }

                var result = await _observationManager.GetGeogridTileAggregationAsync(filter.ToSearchFilter(), zoom, bboxOrError.Value);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error);
                }

                GeoGridResultDto dto = result.Value.ToGeoGridResultDto();
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GeoGridAggregation error.");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Aggregates observations into grid cells. Each grid cell contains the number
        /// of observations and the number of unique taxa (usually species) in the grid cell.
        /// The grid cells are squares in WGS84 coordinate system which means that they also
        /// will be squares in the WGS84 Web Mercator coordinate system.
        /// </summary>
        /// <remarks>
        /// The following table shows the approximate grid cell size (width) in different
        /// coordinate systems for the different zoom levels.
        /// | Zoom level | WGS84    | Web Mercator  |  SWEREF99TM(Southern Sweden) |  SWEREF99TM(North Sweden) |
        /// |------------|----------|---------------|:----------------------------:|:-------------------------:|
        /// | 1          |      180 |       20000km |                       8000km |                   12000km |
        /// | 2          |       90 |       10000km |                       4000km |                    6000km |
        /// | 3          |       45 |        5000km |                       2000km |                    3000km |
        /// | 4          |     22.5 |        2500km |                       1000km |                    1500km |
        /// | 5          |    11.25 |        1250km |                        500km |                     750km |
        /// | 6          |    5.625 |         600km |                        250km |                     360km |
        /// | 7          |   2.8125 |         300km |                        120km |                     180km |
        /// | 8          | 1.406250 |         150km |                         60km |                      90km |
        /// | 9          | 0.703125 |          80km |                         30km |                      45km |
        /// | 10         | 0.351563 |          40km |                         15km |                      23km |
        /// | 11         | 0.175781 |          20km |                          8km |                      11km |
        /// | 12         | 0.087891 |          10km |                          4km |                       6km |
        /// | 13         | 0.043945 |           5km |                          2km |                       3km |
        /// | 14         | 0.021973 |         2500m |                        1000m |                     1400m |
        /// | 15         | 0.010986 |         1200m |                         500m |                      700m |
        /// | 16         | 0.005493 |          600m |                         240m |                      350m |
        /// | 17         | 0.002747 |          300m |                         120m |                      180m |
        /// | 18         | 0.001373 |          150m |                          60m |                       90m |
        /// | 19         | 0.000687 |           80m |                          30m |                       45m |
        /// | 20         | 0.000343 |           40m |                          15m |                       22m |
        /// | 21         | 0.000172 |           19m |                           7m |                       11m |
        /// </remarks>
        /// <param name="filter">The search filter.</param>
        /// <param name="zoom">A zoom level between 1 and 21.</param>
        /// <param name="bboxLeft">Bounding box left (longitude) coordinate in WGS84.</param>
        /// <param name="bboxTop">Bounding box top (latitude) coordinate in WGS84.</param>
        /// <param name="bboxRight">Bounding box right (longitude) coordinate in WGS84.</param>
        /// <param name="bboxBottom">Bounding box bottom (latitude) coordinate in WGS84.</param>
        /// <returns></returns>
        [HttpPost("GeoGridAggregationInternal")]
        [ProducesResponseType(typeof(GeoGridResultDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [InternalApi]
        public async Task<IActionResult> InternalGeogridSearchTileBasedAggregationAsync(
            [FromBody] SearchFilterInternalDto filter,
            [FromQuery] int zoom = 1,
            [FromQuery] double? bboxLeft = null,
            [FromQuery] double? bboxTop = null,
            [FromQuery] double? bboxRight = null,
            [FromQuery] double? bboxBottom = null)
        {
            try
            {
                var filterValidation = ValidateSearchFilterInternal(filter);
                var zoomOrError = ValidateGeogridZoomArgument(zoom, minLimit: 1, maxLimit: 21);
                var bboxOrError = LatLonBoundingBox.Create(bboxLeft, bboxTop, bboxRight, bboxBottom);
                var paramsValidationResult = Result.Combine(filterValidation, zoomOrError, bboxOrError);
                if (paramsValidationResult.IsFailure)
                {
                    return BadRequest(paramsValidationResult.Error);
                }

                var result = await _observationManager.GetGeogridTileAggregationAsync(filter.ToSearchFilterInternal(), zoom, bboxOrError.Value);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error);
                }

                GeoGridResultDto dto = result.Value.ToGeoGridResultDto();
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GeoGridAggregation error.");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Aggregates observations into grid cells and returns a GeoJSON file with all grid cells.
        /// </summary>
        /// <param name="filter">The search filter.</param>
        /// <param name="zoom">A zoom level between 1 and 21.</param>
        /// <param name="bboxLeft">Bounding box left (longitude) coordinate in WGS84.</param>
        /// <param name="bboxTop">Bounding box top (latitude) coordinate in WGS84.</param>
        /// <param name="bboxRight">Bounding box right (longitude) coordinate in WGS84.</param>
        /// <param name="bboxBottom">Bounding box bottom (latitude) coordinate in WGS84.</param>
        /// <returns></returns>
        [HttpPost("GeoGridAggregationGeoJson")]
        [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [InternalApi]
        public async Task<IActionResult> GeogridSearchTileBasedAggregationAsGeoJsonAsync(
            [FromBody] SearchFilterDto filter,
            [FromQuery] int zoom = 1,
            [FromQuery] double? bboxLeft = null,
            [FromQuery] double? bboxTop = null,
            [FromQuery] double? bboxRight = null,
            [FromQuery] double? bboxBottom = null)
        {
            try
            {
                var filterValidation = ValidateSearchFilter(filter);
                var zoomOrError = ValidateGeogridZoomArgument(zoom, minLimit: 1, maxLimit: 21);
                var bboxOrError = LatLonBoundingBox.Create(bboxLeft, bboxTop, bboxRight, bboxBottom);
                var paramsValidationResult = Result.Combine(filterValidation, zoomOrError, bboxOrError);
                if (paramsValidationResult.IsFailure)
                {
                    return BadRequest(paramsValidationResult.Error);
                }

                var result = await _observationManager.GetGeogridTileAggregationAsync(filter.ToSearchFilter(), zoomOrError.Value, bboxOrError.Value);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error);
                }

                string strJson = result.Value.GetFeatureCollectionGeoJson();
                var bytes = Encoding.UTF8.GetBytes(strJson);
                return File(bytes, "application/json", "grid.geojson");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GeoGridAggregationGeoJson error.");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Aggregates observation by taxon. Each item contains the number of observations for the specific taxon.
        /// </summary>
        /// <param name="filter">The search filter.</param>
        /// <param name="skip">Start index of returned records. Skip+Take must be less than or equal to 65535.</param>
        /// <param name="take">End index of returned records. Skip+Take must be less than or equal to 65535.</param>
        /// <param name="bboxLeft">Bounding box left (longitude) coordinate in WGS84.</param>
        /// <param name="bboxTop">Bounding box top (latitude) coordinate in WGS84.</param>
        /// <param name="bboxRight">Bounding box right (longitude) coordinate in WGS84.</param>
        /// <param name="bboxBottom">Bounding box bottom (latitude) coordinate in WGS84.</param>
        /// <returns></returns>
        [HttpPost("TaxonAggregation")]
        [ProducesResponseType(typeof(PagedResultDto<TaxonAggregationItemDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> TaxonAggregationAsync(
            [FromBody] SearchFilterDto filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] double? bboxLeft = null,
            [FromQuery] double? bboxTop = null,
            [FromQuery] double? bboxRight = null,
            [FromQuery] double? bboxBottom = null)
        {
            try
            {
                var filterValidation = ValidateSearchFilter(filter);
                var pagingArgumentsValidation = ValidatePagingArguments(skip, take);
                var bboxOrError = LatLonBoundingBox.Create(bboxLeft, bboxTop, bboxRight, bboxBottom);
                var paramsValidationResult = Result.Combine(filterValidation, pagingArgumentsValidation, bboxOrError);
                if (paramsValidationResult.IsFailure)
                {
                    return BadRequest(paramsValidationResult.Error);
                }

                var result = await _observationManager.GetTaxonAggregationAsync(filter.ToSearchFilter(), bboxOrError.Value, skip, take);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error);
                }

                PagedResultDto<TaxonAggregationItemDto> dto = result.Value.ToPagedResultDto(result.Value.Records.ToTaxonAggregationItemDtos());
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "TaxonAggregation error.");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Aggregates observation by taxon. Each item contains the number of observations for the specific taxon.
        /// </summary>
        /// <param name="filter">The search filter.</param>
        /// <param name="skip">Start index of returned records. Skip+Take must be less than or equal to 65535.</param>
        /// <param name="take">End index of returned records. Skip+Take must be less than or equal to 65535.</param>
        /// <param name="bboxLeft">Bounding box left (longitude) coordinate in WGS84.</param>
        /// <param name="bboxTop">Bounding box top (latitude) coordinate in WGS84.</param>
        /// <param name="bboxRight">Bounding box right (longitude) coordinate in WGS84.</param>
        /// <param name="bboxBottom">Bounding box bottom (latitude) coordinate in WGS84.</param>
        /// <returns></returns>
        [HttpPost("TaxonAggregationInternal")]
        [ProducesResponseType(typeof(PagedResultDto<TaxonAggregationItemDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [InternalApi]
        public async Task<IActionResult> TaxonAggregationInternalAsync(
            [FromBody] SearchFilterInternalDto filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] double? bboxLeft = null,
            [FromQuery] double? bboxTop = null,
            [FromQuery] double? bboxRight = null,
            [FromQuery] double? bboxBottom = null)
        {
            try
            {
                var filterValidation = ValidateSearchFilterInternal(filter);
                var pagingArgumentsValidation = ValidatePagingArguments(skip, take);
                var bboxOrError = LatLonBoundingBox.Create(bboxLeft, bboxTop, bboxRight, bboxBottom);
                var paramsValidationResult = Result.Combine(filterValidation, pagingArgumentsValidation, bboxOrError);
                if (paramsValidationResult.IsFailure)
                {
                    return BadRequest(paramsValidationResult.Error);
                }

                var result = await _observationManager.GetTaxonAggregationAsync(filter.ToSearchFilterInternal(), bboxOrError.Value, skip, take);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error);
                }

                PagedResultDto<TaxonAggregationItemDto> dto = result.Value.ToPagedResultDto(result.Value.Records.ToTaxonAggregationItemDtos());
                return new OkObjectResult(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "TaxonAggregation error.");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }


        [HttpGet("Provider/{providerId}/lastmodified")]
        [ProducesResponseType(typeof(IEnumerable<DateTime>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetLatestModifiedDateForProviderAsync([FromRoute] int providerId)
        {
            try
            {
                return new OkObjectResult(await _observationManager.GetLatestModifiedDateForProviderAsync(providerId));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error getting last modified date for provider {providerId}");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        private Result<int> ValidateGeogridZoomArgument(int zoom, int minLimit, int maxLimit)
        {
            if (zoom < minLimit || zoom > maxLimit)
            {
                return Result.Failure<int>($"Zoom must be between {minLimit} and {maxLimit}");
            }

            return Result.Success(zoom);
        }

        private Result ValidatePagingArguments(int skip, int take)
        {
            if (skip < 0) return Result.Failure("Skip must be 0 or greater.");
            if (take <= 0) return Result.Failure("Take must be greater than 0");
            if (skip + take > _observationManager.MaxNrElasticSearchAggregationBuckets)
                return Result.Failure($"Skip+Take={skip + take}. Skip+Take must be less than or equal to {_observationManager.MaxNrElasticSearchAggregationBuckets}.");

            return Result.Success();
        }

        /// <summary>
        /// Basic validation of search filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        private Tuple<bool, IEnumerable<string>> ValidateFilter(SearchFilter filter, int skip, int take)
        {
            var errors = new List<string>();

            if (!filter.IsFilterActive)
            {
                errors.Add("You must provide a filter."); ;
            }
            else
            {

                // No culture code, set default
                if (string.IsNullOrEmpty(filter?.FieldTranslationCultureCode))
                {
                    filter.FieldTranslationCultureCode = "sv-SE";
                }

                if (!new[] { "sv-SE", "en-GB" }.Contains(filter.FieldTranslationCultureCode,
                    StringComparer.CurrentCultureIgnoreCase))
                {
                    errors.Add("Unknown FieldTranslationCultureCode. Supported culture codes, sv-SE, en-GB");
                }

                //Remove the limitations if we use the internal functions
                if (!(filter is SearchFilterInternal))
                {
                    if (skip < 0 || take <= 0 || take > MaxBatchSize)
                    {
                        errors.Add($"You can't take more than {MaxBatchSize} at a time.");
                    }
                }

                if (skip + take > ElasticSearchMaxRecords)
                {
                    errors.Add($"Skip + take can't be greater than { ElasticSearchMaxRecords }");
                }
            }


            return new Tuple<bool, IEnumerable<string>>(!errors.Any(), errors);
        }

        private Result ValidateSearchFilter(SearchFilterDto filter)
        {
            var errors = new List<string>();

            // No culture code, set default
            if (string.IsNullOrEmpty(filter?.TranslationCultureCode))
            {
                filter.TranslationCultureCode = "sv-SE";
            }

            if (!new[] { "sv-SE", "en-GB" }.Contains(filter.TranslationCultureCode,
                StringComparer.CurrentCultureIgnoreCase))
            {
                errors.Add("Unknown FieldTranslationCultureCode. Supported culture codes, sv-SE, en-GB");
            }

            if (errors.Count > 0) return Result.Failure(string.Join(". ", errors));
            return Result.Success();
        }

        private Result ValidateSearchFilterInternal(SearchFilterInternalDto filter)
        {
            var errors = new List<string>();

            // No culture code, set default
            if (string.IsNullOrEmpty(filter?.TranslationCultureCode))
            {
                filter.TranslationCultureCode = "sv-SE";
            }

            if (!new[] { "sv-SE", "en-GB" }.Contains(filter.TranslationCultureCode,
                StringComparer.CurrentCultureIgnoreCase))
            {
                errors.Add("Unknown FieldTranslationCultureCode. Supported culture codes, sv-SE, en-GB");
            }

            if (errors.Count > 0) return Result.Failure(string.Join(". ", errors));
            return Result.Success();
        }

        private Result ValidateSearchPagingArguments(int skip, int take)
        {
            var errors = new List<string>();

            if (skip < 0 || take <= 0 || take > MaxBatchSize)
            {
                errors.Add($"You can't take more than {MaxBatchSize} at a time.");
            }

            if (skip + take > ElasticSearchMaxRecords)
            {
                errors.Add($"Skip + take can't be greater than { ElasticSearchMaxRecords }");
            }

            if (errors.Count > 0) return Result.Failure(string.Join(". ", errors));
            return Result.Success();
        }

        private Result ValidateSearchPagingArgumentsInternal(int skip, int take)
        {
            var errors = new List<string>();

            if (skip + take > ElasticSearchMaxRecords)
            {
                errors.Add($"Skip + take can't be greater than { ElasticSearchMaxRecords }");
            }

            if (errors.Count > 0) return Result.Failure(string.Join(". ", errors));
            return Result.Success();
        }
    }
}