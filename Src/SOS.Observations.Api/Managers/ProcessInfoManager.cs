﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SOS.Lib.Models.Processed.ProcessInfo;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Observations.Api.Managers.Interfaces;

namespace SOS.Observations.Api.Managers
{
    /// <summary>
    ///     Process info manager
    /// </summary>
    public class ProcessInfoManager : IProcessInfoManager
    {
        private readonly ILogger<ProcessInfoManager> _logger;
        private readonly IProcessInfoRepository _processInfoRepository;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="processInfoRepository"></param>
        /// <param name="logger"></param>
        public ProcessInfoManager(
            IProcessInfoRepository processInfoRepository,
            ILogger<ProcessInfoManager> logger)
        {
            _processInfoRepository = processInfoRepository ??
                                     throw new ArgumentNullException(nameof(processInfoRepository));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ProcessInfo> GetProcessInfoAsync(bool active)
        {
            try
            {
                var processInfo = await _processInfoRepository.GetProcessInfoAsync(active);
                return processInfo;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get current process info");
                return null;
            }
        }
    }
}