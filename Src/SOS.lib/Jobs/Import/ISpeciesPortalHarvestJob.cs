﻿using System.Threading.Tasks;
using Hangfire;

namespace SOS.Lib.Jobs.Import
{
    public interface ISpeciesPortalHarvestJob
    {
        /// <summary>
        /// Run species portal harvest
        /// </summary>
        /// <returns></returns>
        Task<bool> RunAsync(IJobCancellationToken cancellationToken);
    }
}