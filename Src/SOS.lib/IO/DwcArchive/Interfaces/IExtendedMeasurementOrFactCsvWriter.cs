﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using NReco.Csv;
using SOS.Export.Models;
using SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Search;
using SOS.Lib.Repositories.Processed.Interfaces;

namespace SOS.Lib.IO.DwcArchive.Interfaces
{
    public interface IExtendedMeasurementOrFactCsvWriter
    {
        /// <summary>
        /// Create a Emof CSV file.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="stream"></param>
        /// <param name="fieldDescriptions"></param>
        /// <param name="processedObservationRepository"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CreateCsvFileAsync(FilterBase filter, Stream stream,
            IEnumerable<FieldDescription> fieldDescriptions,
            IProcessedObservationRepository processedObservationRepository,
            IJobCancellationToken cancellationToken);

        /// <summary>
        /// Create a headerless Emof CSV file.
        /// </summary>
        /// <param name="emofRows"></param>
        /// <param name="streamWriter"></param>
        /// <param name="writeEventId"></param>
        /// <returns></returns>
        Task WriteHeaderlessEmofCsvFileAsync(
            IEnumerable<ExtendedMeasurementOrFactRow> emofRows,
            StreamWriter streamWriter,
            bool writeEventId = false);

        /// <summary>
        /// Write Emof header row.
        /// </summary>
        /// <param name="csvWriter"></param>
        /// <param name="isEventCore"></param>
        void WriteHeaderRow(CsvWriter csvWriter, bool isEventCore = false);
    }
}