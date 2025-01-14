﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SOS.Lib.Jobs.Process;
using SOS.Process.Managers.Interfaces;

namespace SOS.Process.Jobs
{
    /// <summary>
    ///     Artportalen harvest
    /// </summary>
    public class ActivateInstanceJob : IActivateInstanceJob
    {
        private readonly IInstanceManager _instanceManager;
        private readonly ILogger<ActivateInstanceJob> _logger;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="instanceManager"></param>
        /// <param name="logger"></param>
        public ActivateInstanceJob(
            IInstanceManager instanceManager,
            ILogger<ActivateInstanceJob> logger)
        {
            _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        [DisplayName("Activate passed ElasticSearch instance")]
        public async Task<bool> RunAsync(byte instance)
        {
            // Activate passed instance
            var success = await _instanceManager.SetActiveInstanceAsync(instance);
            return success ? true : throw new Exception("Activate instance job failed");
        }
    }
}