﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DwC_A;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Conventions;
using Newtonsoft.Json;
using SOS.Import.Harvesters.Interfaces;
using SOS.Import.Managers.Interfaces;
using SOS.Import.Repositories.Destination.Interfaces;
using SOS.Lib.Configuration.Import;
using SOS.Lib.Enums;
using SOS.Lib.Helpers;
using SOS.Lib.Jobs.Import;
using SOS.Lib.Json;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.DarwinCore;
using SOS.Lib.Models.Verbatim.Shared;
using SOS.Lib.Repositories.Processed.Interfaces;

namespace SOS.Import.Jobs
{
    /// <summary>
    ///     Create Dwc-A data validation report job.
    /// </summary>
    public class CreateDwcaDataValidationReportJob : ICreateDwcaDataValidationReportJob
    {
        private readonly IDwcaDataValidationReportManager _dwcaDataValidationReportManager;
        private readonly DwcaConfiguration _dwcaConfiguration;
        private readonly ILogger<CreateDwcaDataValidationReportJob> _logger;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="dwcaDataValidationReportManager"></param>
        /// <param name="dwcaConfiguration"></param>
        /// <param name="logger"></param>
        /// <param name="processedTaxonRepository"></param>
        public CreateDwcaDataValidationReportJob(IDwcaDataValidationReportManager dwcaDataValidationReportManager,
            DwcaConfiguration dwcaConfiguration,
            ILogger<CreateDwcaDataValidationReportJob> logger, IProcessedTaxonRepository processedTaxonRepository)
        {
            _dwcaDataValidationReportManager = dwcaDataValidationReportManager ?? throw new ArgumentNullException(nameof(dwcaDataValidationReportManager));
            _dwcaConfiguration = dwcaConfiguration ?? throw new ArgumentNullException(nameof(dwcaConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> RunAsync(
            string archivePath,
            IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Start DwC-A Test Import Job");
                string savePath = Path.Combine(_dwcaConfiguration.ImportPath, $"DwcaDataValidationReport-{Path.GetFileNameWithoutExtension(archivePath)}.json");
                using var archiveReader = new ArchiveReader(archivePath, _dwcaConfiguration.ImportPath);
                var dataValidationSummary = await _dwcaDataValidationReportManager.CreateDataValidationSummary(archiveReader);
                var jsonSettings = CreateJsonSerializerSettings();
                var strJson = JsonConvert.SerializeObject(dataValidationSummary, Formatting.Indented, jsonSettings);
                if (File.Exists(savePath)) File.Delete(savePath);
                await File.WriteAllTextAsync(savePath, strJson);
                return savePath;
            }
            finally
            {
                if (File.Exists(archivePath)) File.Delete(archivePath);
            }
        }

        private JsonSerializerSettings CreateJsonSerializerSettings()
        {
            // Exclude some properties.
            var jsonResolver = new IgnorableSerializerContractResolver()
                .Ignore<ProcessedObservation>(obs => obs.Location.Point)
                .Ignore<ProcessedObservation>(obs => obs.Location.PointWithBuffer)
                .Ignore<ProcessedObservation>(obs => obs.IsInEconomicZoneOfSweden)
                .Ignore<DwcObservationVerbatim>(obs => obs.RecordId)
                .Ignore<DwcObservationVerbatim>(obs => obs.Id)
                .Ignore<DwcObservationVerbatim>(obs => obs.DataProviderId)
                .Ignore<DwcObservationVerbatim>(obs => obs.DataProviderIdentifier)
                .KeepTypeWithDefaultValue(typeof(ProcessedFieldMapValue)); // Keep properties of ProcessedFieldMapValue type even if it have default value.

            var jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = jsonResolver,
                DefaultValueHandling = DefaultValueHandling.Ignore // Exclude properties with default values.
            };

            return jsonSettings;
        }
    }
}