﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using SOS.Export.IO.DwcArchive.Interfaces;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Enums;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Mvm;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Lib.Repositories.Verbatim.Interfaces;
using SOS.Process.Processors.Mvm.Interfaces;

namespace SOS.Process.Processors.Mvm
{
    /// <summary>
    ///     Process factory class
    /// </summary>
    public class MvmObservationProcessor : ObservationProcessorBase<MvmObservationProcessor, MvmObservationVerbatim, MvmObservationFactory, IMvmObservationVerbatimRepository>, IMvmObservationProcessor
    {
        private readonly IAreaHelper _areaHelper;
        private readonly IMvmObservationVerbatimRepository _mvmObservationVerbatimRepository;

        /// <inheritdoc />
        protected override async Task<(int publicCount, int protectedCount)> ProcessObservations(
            DataProvider dataProvider,
            IDictionary<int, Lib.Models.Processed.Observation.Taxon> taxa,
            JobRunModes mode,
            IJobCancellationToken cancellationToken)
        {
            var observationFactory = new MvmObservationFactory(dataProvider, taxa, _areaHelper);

            return await base.ProcessObservationsAsync(
                dataProvider,
                mode,
                observationFactory,
                _mvmObservationVerbatimRepository,
                cancellationToken);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mvmObservationVerbatimRepository"></param>
        /// <param name="areaHelper"></param>
        /// <param name="processedPublicObservationRepository"></param>
        /// <param name="vocabularyValueResolver"></param>
        /// <param name="dwcArchiveFileWriterCoordinator"></param>
        /// <param name="validationManager"></param>
        /// <param name="processConfiguration"></param>
        /// <param name="logger"></param>
        public MvmObservationProcessor(IMvmObservationVerbatimRepository mvmObservationVerbatimRepository,
            IAreaHelper areaHelper,
            IProcessedPublicObservationRepository processedPublicObservationRepository,
            IVocabularyValueResolver vocabularyValueResolver,
            IDwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator,
            IValidationManager validationManager,
            ProcessConfiguration processConfiguration,
            ILogger<MvmObservationProcessor> logger) :
            base(processedPublicObservationRepository, vocabularyValueResolver, dwcArchiveFileWriterCoordinator, validationManager, processConfiguration, logger)
        {
            _mvmObservationVerbatimRepository = mvmObservationVerbatimRepository ??
                                                throw new ArgumentNullException(
                                                    nameof(mvmObservationVerbatimRepository));
            _areaHelper = areaHelper ?? throw new ArgumentNullException(nameof(areaHelper));
        }

        public override DataProviderType Type => DataProviderType.MvmObservations;
    }
}