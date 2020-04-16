﻿using System.Threading.Tasks;
using Hangfire;
using SOS.Lib.Models.Verbatim.Shared;

namespace SOS.Import.Harvesters.Observations.Interfaces
{
    public interface ISersObservationHarvester
    {
        /// <summary>
        /// Harvest Observations
        /// </summary>
        /// <returns></returns>
        Task<HarvestInfo> HarvestObservationsAsync(IJobCancellationToken cancellationToken);
    }
}
