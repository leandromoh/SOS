﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Enums;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.IO.DwcArchive.Interfaces;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Nors;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Lib.Repositories.Verbatim.Interfaces;
using SOS.Process.Managers.Interfaces;
using SOS.Process.Processors.Nors.Interfaces;

namespace SOS.Process.Processors.Nors
{
    /// <summary>
    ///     Process factory class
    /// </summary>
    public class NorsObservationProcessor : 
        ObservationProcessorBase<NorsObservationProcessor, NorsObservationVerbatim, INorsObservationVerbatimRepository>,
        INorsObservationProcessor
    {
        private readonly IAreaHelper _areaHelper;
        private readonly INorsObservationVerbatimRepository _norsObservationVerbatimRepository;

        /// <inheritdoc />
        protected override async Task<(int publicCount, int protectedCount)> ProcessObservations(
            DataProvider dataProvider,
            IDictionary<int, Lib.Models.Processed.Observation.Taxon> taxa,
            JobRunModes mode,
            IJobCancellationToken cancellationToken)
        {
            var observationFactory = new NorsObservationFactory(dataProvider, taxa, _areaHelper);

            return await base.ProcessObservationsAsync(
                dataProvider,
                mode,
                observationFactory,
                _norsObservationVerbatimRepository,
                cancellationToken);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="norsObservationVerbatimRepository"></param>
        /// <param name="areaHelper"></param>
        /// <param name="processedObservationRepository"></param>
        /// <param name="vocabularyValueResolver"></param>
        /// <param name="dwcArchiveFileWriterCoordinator"></param>
        /// <param name="processManager"></param>
        /// <param name="validationManager"></param>
        /// <param name="diffusionManager"></param>
        /// <param name="processConfiguration"></param>
        /// <param name="logger"></param>
        public NorsObservationProcessor(INorsObservationVerbatimRepository norsObservationVerbatimRepository,
            IAreaHelper areaHelper,
            IProcessedObservationRepository processedObservationRepository,
            IVocabularyValueResolver vocabularyValueResolver,
            IDwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator,
            IProcessManager processManager,
            IValidationManager validationManager,
            IDiffusionManager diffusionManager,
            ProcessConfiguration processConfiguration,
            ILogger<NorsObservationProcessor> logger) :
            base(processedObservationRepository, vocabularyValueResolver, dwcArchiveFileWriterCoordinator, processManager, validationManager, diffusionManager, processConfiguration, logger)
        {
            _norsObservationVerbatimRepository = norsObservationVerbatimRepository ??
                                                 throw new ArgumentNullException(
                                                     nameof(norsObservationVerbatimRepository));
            _areaHelper = areaHelper ?? throw new ArgumentNullException(nameof(areaHelper));
        }

        public override DataProviderType Type => DataProviderType.NorsObservations;
    }
}