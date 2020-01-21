﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using SOS.Export.Models;
using SOS.Export.Repositories.Interfaces;
using SOS.Lib.Models.Search;

namespace SOS.Export.IO.DwcArchive.Interfaces
{
    public interface IExtendedMeasurementOrFactCsvWriter
    {
        /// <inheritdoc />
        Task<bool> CreateCsvFileAsync(AdvancedFilter filter, Stream stream,
            IEnumerable<FieldDescription> fieldDescriptions,
            IProcessedSightingRepository processedSightingRepository,
            IJobCancellationToken cancellationToken);
    }
}