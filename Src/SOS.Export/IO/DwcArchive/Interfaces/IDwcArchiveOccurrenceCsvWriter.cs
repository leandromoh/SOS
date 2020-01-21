﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using SOS.Export.Models;
using SOS.Export.Repositories.Interfaces;
using SOS.Lib.Models.Search;

namespace SOS.Export.IO.DwcArchive.Interfaces
{
    public interface IDwcArchiveOccurrenceCsvWriter
    {
        /// <summary>
        /// Creates a DwC occurrence CSV file.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="stream"></param>
        /// <param name="fieldDescriptions"></param>
        /// <param name="processedSightingRepository"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CreateOccurrenceCsvFileAsync(
            AdvancedFilter filter,
            Stream stream,
            IEnumerable<FieldDescription> fieldDescriptions,
            IProcessedSightingRepository processedSightingRepository,
            IJobCancellationToken cancellationToken);
    }
}