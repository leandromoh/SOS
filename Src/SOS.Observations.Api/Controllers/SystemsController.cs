﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SOS.Observations.Api.Controllers.Interfaces;
using SOS.Observations.Api.Dtos;
using SOS.Observations.Api.Managers.Interfaces;
using SOS.Observations.Api.Swagger;

namespace SOS.Observations.Api.Controllers
{
    /// <summary>
    ///     Sighting controller
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class SystemsController : ControllerBase, ISystemsController
    {
        private readonly ILogger<SystemsController> _logger;
        private readonly IProcessInfoManager _processInfoManager;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="processInfoManager"></param>
        /// <param name="logger"></param>
        public SystemsController(IProcessInfoManager processInfoManager, ILogger<SystemsController> logger)
        {
            _processInfoManager = processInfoManager ?? throw new ArgumentNullException(nameof(processInfoManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        [HttpGet("ProcessInformation")]
        [ProducesResponseType(typeof(ProcessInfoDto), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.InternalServerError)]
        [InternalApi]
        public async Task<IActionResult> GetProcessInfo([FromQuery] bool active)
        {
            try
            {
                return new OkObjectResult(await _processInfoManager.GetProcessInfoAsync(active));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting process information");
                return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
            }
        }
    }
}