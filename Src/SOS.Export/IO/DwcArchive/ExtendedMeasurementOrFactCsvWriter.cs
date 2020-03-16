﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SOS.Export.Extensions;
using SOS.Export.Mappings;
using SOS.Export.Models;
using SOS.Export.Repositories.Interfaces;
using  SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Processed.Sighting;
using SOS.Lib.Models.Search;

namespace SOS.Export.IO.DwcArchive
{
    public class ExtendedMeasurementOrFactCsvWriter : Interfaces.IExtendedMeasurementOrFactCsvWriter
    {
        private readonly ILogger<ExtendedMeasurementOrFactCsvWriter> _logger;

        public ExtendedMeasurementOrFactCsvWriter(ILogger<ExtendedMeasurementOrFactCsvWriter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<bool> CreateCsvFileAsync(
            FilterBase filter, 
            Stream stream,
            IEnumerable<FieldDescription> fieldDescriptions,
            IProcessedObservationRepository processedObservationRepository,
            IJobCancellationToken cancellationToken)
        {
            try
            {
                var skip = 0;
                const int take = 100000;
                var map = new ExtendedMeasurementOrFactRowMap();
                IEnumerable<ProcessedProject> projectParameters = await processedObservationRepository.GetProjectParameters(filter, skip, take);

                while (projectParameters.Any())
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                    IEnumerable<ExtendedMeasurementOrFactRow> records = projectParameters.ToExtendedMeasurementOrFactRows();
                    await WriteEmofCsvAsync(stream, records, map);
                    skip += take;
                    projectParameters = await processedObservationRepository.GetProjectParameters(filter, skip, take);
                }

                return true;
            }
            catch (JobAbortedException)
            {
                _logger.LogInformation($"{nameof(CreateCsvFileAsync)} was canceled.");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to create ExtendedMeasurementOrFact CSV file.");
                return false;
            }
        }

        private async Task WriteEmofCsvAsync<T>(Stream stream, IEnumerable<ExtendedMeasurementOrFactRow> records, ClassMap<T> map)
        {
            if (!records?.Any() ?? true)
            {
                return;
            }

            await using var streamWriter = new StreamWriter(stream, null, -1, false);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = "\t", // tab
                Encoding = System.Text.Encoding.UTF8
            };
            using var csv = new CsvWriter(streamWriter, csvConfig);

            csv.Configuration.RegisterClassMap(map);
            csv.WriteRecords(records);
        }
    }
}
