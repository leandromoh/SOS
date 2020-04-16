﻿using System.Threading.Tasks;
using Hangfire;

namespace SOS.Lib.Jobs.Import
{
    public interface ISersHarvestJob
    {
        /// <summary>
        /// Run KUL harvest.
        /// </summary>
        /// <returns></returns>
        Task<bool> RunAsync(IJobCancellationToken cancellationToken);
    }
}
