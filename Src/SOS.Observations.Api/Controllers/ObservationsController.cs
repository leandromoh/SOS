﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;
using SOS.Observations.Api.Controllers.Interfaces;
using SOS.Observations.Api.Managers.Interfaces;

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
        private readonly IFieldMappingManager _fieldMappingManager;
        private readonly ILogger<ObservationsController> _logger;
        private readonly IObservationManager _observationManager;

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
                    errors.Add($"Skip + take can't be greater than { ElasticSearchMaxRecords }" );
                }
            }


            return new Tuple<bool, IEnumerable<string>>(!errors.Any(), errors);
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="observationManager"></param>
        /// <param name="fieldMappingManager"></param>
        /// <param name="areaManager"></param>
        /// <param name="logger"></param>
        public ObservationsController(
            IObservationManager observationManager,
            IFieldMappingManager fieldMappingManager,
            ILogger<ObservationsController> logger)
        {
            _observationManager = observationManager ?? throw new ArgumentNullException(nameof(observationManager));
            _fieldMappingManager = fieldMappingManager ?? throw new ArgumentNullException(nameof(fieldMappingManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResult<ProcessedObservation>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetChunkAsync([FromBody] SearchFilter filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] string sortBy = "",
            [FromQuery] SearchSortOrder sortOrder = SearchSortOrder.Asc)
        {
            try
            {
                var validateResult = ValidateFilter(filter, skip, take);
                if (!validateResult.Item1)
                {
                    return BadRequest( string.Join(". ", validateResult.Item2));
                }

                return new OkObjectResult(
                    await _observationManager.GetChunkAsync(filter, skip, take, sortBy, sortOrder));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting batch of sightings");
                return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc />
        [HttpGet("TermDictionary")]
        [ProducesResponseType(typeof(IEnumerable<FieldMapping>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetFieldMappingAsync()
        {
            try
            {
                return new OkObjectResult(await _fieldMappingManager.GetFieldMappingsAsync());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting field mappings");
                return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc />
        [HttpPost("searchinternal")]
        [ProducesResponseType(typeof(PagedResult<ProcessedObservation>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.InternalServerError)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetChunkInternalAsync([FromBody] SearchFilterInternal filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            [FromQuery] string sortBy = "",
            [FromQuery] SearchSortOrder sortOrder = SearchSortOrder.Asc)
        {
            var validateResult = ValidateFilter(filter, skip, take);
            if (!validateResult.Item1)
            {
                return BadRequest(string.Join(". ", validateResult.Item2));
            }

            return await GetChunkAsync(filter, skip, take, sortBy, sortOrder);
        }

        /// <inheritdoc />
        [HttpPost("searchaggregatedinternal")]
        [ProducesResponseType(typeof(PagedResult<ProcessedObservation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetChunkAggregatedInternalAsync([FromBody] SearchFilterInternal filter, [FromQuery] AggregationType aggregationType)
        {
            try
            {
                var (isValid, validationErrors) = ValidateFilter(filter, 0, 1);
                if (!isValid)
                {
                    return BadRequest(string.Join(". ", validationErrors));
                }

                return new OkObjectResult(await _observationManager.GetAggregatedChunkAsync(filter, aggregationType));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting batch of aggregated sightings");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}