﻿using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SOS.Administration.Api.Models;

namespace SOS.Administration.Api.Controllers
{
    /// <summary>
    /// Environment controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentController : ControllerBase
    {
        /// <summary>
        /// Hosting environment
        /// </summary>
        public IWebHostEnvironment WebHostEnvironment { get; }

        private readonly ILogger<EnvironmentController> _logger;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <param name="logger"></param>
        public EnvironmentController(
            IWebHostEnvironment webHostEnvironment,
            ILogger<EnvironmentController> logger
            )
        {
            WebHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
        }

        /// <summary>
        /// Gets the running enviroment.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        [ProducesResponseType(typeof(EnvironmentInformationDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public IActionResult GetEnvironmentInformation()
        {
            try
            {
                EnvironmentInformationDto environmentInformationDto = new EnvironmentInformationDto()
                {
                    EnvironmentType = WebHostEnvironment.EnvironmentName,
                    HostingServerName = Environment.MachineName,
                    OsPlatform = RuntimeInformation.OSDescription,
                    AspDotnetVersion = Assembly
                        .GetEntryAssembly()?
                        .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?
                        .FrameworkName
                };

                return new OkObjectResult(environmentInformationDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Retrieving environment information failed");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}