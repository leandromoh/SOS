﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SOS.Export.Managers.Interfaces;
using SOS.Lib.Jobs.Export;
using SOS.Lib.Models.Search;

namespace SOS.Export.Jobs
{
    /// <summary>
    ///     Artportalen harvest
    /// </summary>
    public class ExportAndSendJob : IExportAndSendJob
    {
        private readonly ILogger<ExportAndSendJob> _logger;
        private readonly IObservationManager _observationManager;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="observationManager"></param>
        /// <param name="logger"></param>
        public ExportAndSendJob(IObservationManager observationManager, ILogger<ExportAndSendJob> logger)
        {
            _observationManager = observationManager ?? throw new ArgumentNullException(nameof(observationManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        [DisplayName("Create a DwC-A file using passed filter and send an email when file is ready to download")]
        public async Task<bool> RunAsync(ExportFilter filter, string email, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Start export and send job");
                var success = await _observationManager.ExportAndSendAsync(filter, email, cancellationToken);

                _logger.LogInformation($"End export and send job. Success: {success}");

                return success ? true : throw new Exception("Export and send job failed");
            }
            catch (JobAbortedException)
            {
                _logger.LogInformation("Export and send job was cancelled.");
                return false;
            }
        }
    }
}