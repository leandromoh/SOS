﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SOS.Observations.Api.Managers.Interfaces;

namespace SOS.Observations.Api.HealthChecks
{
    /// <summary>
    /// Health check by checking number of documents in index 
    /// </summary>
    public class DuplicateHealthCheck : IHealthCheck
    {
        private readonly IObservationManager _observationManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="observationManager"></param>
        public DuplicateHealthCheck(IObservationManager observationManager)
        {
            _observationManager = observationManager ?? throw new ArgumentNullException(nameof(observationManager));
        }

        /// <summary>
        /// Make health check
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                const int maxItems = 20;
                var healthTasks = new[]
                {
                    _observationManager.TryToGetOccurenceIdDuplicatesAsync(true, false, maxItems),
                    _observationManager.TryToGetOccurenceIdDuplicatesAsync(true, true, maxItems),
                    _observationManager.TryToGetOccurenceIdDuplicatesAsync(false, false, maxItems),
                    _observationManager.TryToGetOccurenceIdDuplicatesAsync(false, true, maxItems)
                };

                await Task.WhenAll(healthTasks);

                var activePublicIndexDuplicates = healthTasks[0].Result;
                var activePublicprotectedIndexDuplicates = healthTasks[1].Result;
                var inActivePublicIndexDuplicates = healthTasks[2].Result;
                var inActivePublicprotectedIndexDuplicates = healthTasks[3].Result;

                var errors = new StringBuilder();
                if (activePublicIndexDuplicates?.Any() ?? false)
                {
                    errors.Append($"Duplicates found in active public index: {string.Concat(activePublicIndexDuplicates, ",")}...");
                }

                if (activePublicprotectedIndexDuplicates?.Any() ?? false)
                {
                    errors.Append($"Duplicates found in active protected index: {string.Concat(activePublicprotectedIndexDuplicates, ",")}...");
                }

                if (inActivePublicIndexDuplicates?.Any() ?? false)
                {
                    errors.Append($"Duplicates found in inactive public index: {string.Concat(inActivePublicIndexDuplicates, ",")}...");
                }

                if (inActivePublicprotectedIndexDuplicates?.Any() ?? false)
                {
                    errors.Append($"Duplicates found in inactive protected index: {string.Concat(inActivePublicprotectedIndexDuplicates, ",")}...");
                }

                if (errors.Length != 0)
                {
                    return new HealthCheckResult(HealthStatus.Unhealthy, string.Join(", ", errors));
                }

                return new HealthCheckResult(HealthStatus.Healthy, "No duplicate observations found");
            }
            catch (Exception e)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, "Duplicate health check failed");
            }
        }
    }
}
