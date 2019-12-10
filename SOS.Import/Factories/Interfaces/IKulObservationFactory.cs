﻿using System.Threading.Tasks;
using Hangfire;
using SOS.Lib.Models.Verbatim.Shared;

namespace SOS.Import.Factories.Interfaces
{
    public interface IKulObservationFactory
    {
        /// <summary>
        /// Aggregate sightings.
        /// </summary>
        /// <returns></returns>
        Task<HarvestInfo> HarvestObservationsAsync(IJobCancellationToken cancellationToken);
    }
}
