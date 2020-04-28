﻿using System;
using System.Collections.Generic;
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
using SOS.Observations.Api.Models.Area;

namespace SOS.Observations.Api.Controllers
{
    /// <summary>
    /// Observation controller
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ObservationController : ControllerBase, IObservationController
    {
        private readonly IObservationManager _observationManager;
        private readonly IFieldMappingManager _fieldMappingManager;
        private readonly IAreaManager _areaManager;
        private readonly ILogger<ObservationController> _logger;
        private const int MaxBatchSize = 1000;
        private const int ElasticSearchMaxRecords = 10000;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="observationManager"></param>
        /// <param name="fieldMappingManager"></param>
        /// <param name="logger"></param>
        public ObservationController(
            IObservationManager observationManager, 
            IFieldMappingManager fieldMappingManager,
            IAreaManager areaManager,
            ILogger<ObservationController> logger)
        {
            _observationManager = observationManager ?? throw new ArgumentNullException(nameof(observationManager));
            _fieldMappingManager = fieldMappingManager ?? throw new ArgumentNullException(nameof(fieldMappingManager));
            _areaManager = areaManager ?? throw new ArgumentNullException(nameof(areaManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResult<ProcessedObservation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetChunkAsync([FromBody] SearchFilter filter, [FromQuery]int skip = 0, [FromQuery]int take = 100)
        {
            try
            {
                if (!filter.IsFilterActive )
                {
                    return BadRequest("You must provide a filter.");
                }

                if (skip < 0 || take <= 0 || take > MaxBatchSize)
                {
                    return BadRequest($"You can't take more than { MaxBatchSize } at a time.");
                }

                if (skip + take > ElasticSearchMaxRecords)
                {
                    return BadRequest($"Skip + take ");
                }

                return new OkObjectResult(await _observationManager.GetChunkAsync(filter, skip, take));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting batch of sightings");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc />
        [HttpPost("searchinternal")]
        [ProducesResponseType(typeof(PagedResult<ProcessedObservation>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetChunkInternalAsync([FromBody] SearchFilterInternal filter, [FromQuery]int skip = 0, [FromQuery]int take = 100)
        {
            return await GetChunkAsync(filter, skip, take);            
        }

        /// <inheritdoc />
        [HttpGet("FieldMapping")]
        [ProducesResponseType(typeof(IEnumerable<FieldMapping>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetFieldMappingAsync()
        {
            try
            {
                return new OkObjectResult(await _fieldMappingManager.GetFieldMappingsAsync());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting field mappings");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
        /// <inheritdoc />
        [HttpGet("Areas")]
        [ProducesResponseType(typeof(PagedResult<ExternalArea>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAreasAsync([FromQuery] AreaType areaType, [FromQuery]string searchString, [FromQuery]int skip = 0, [FromQuery]int take = 100)
        {
            try 
            {
                return new OkObjectResult(await _areaManager.GetAreasAsync(areaType, searchString, skip, take));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting areas");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}