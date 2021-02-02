﻿using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SOS.Lib.Enums;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.DataValidation;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.VirtualHerbarium;
using SOS.Lib.Repositories.Resource.Interfaces;
using SOS.Lib.Repositories.Verbatim.Interfaces;
using SOS.Process.Processors.VirtualHerbarium;
using VocabularyValue = SOS.Lib.Models.Processed.Observation.VocabularyValue;

namespace SOS.Import.Factories.Validation
{
    /// <summary>
    /// Virtual Herbarium data validation manager.
    /// </summary>
    public class VirtualHerbariumValidationReportFactory : DataValidationReportFactoryBase<VirtualHerbariumObservationVerbatim>
    {
        private readonly IVirtualHerbariumObservationVerbatimRepository _virtualHerbariumObservationVerbatimRepository;
        private VirtualHerbariumObservationFactory _virtualHerbariumObservationFactory;

        public VirtualHerbariumValidationReportFactory(
            IVocabularyRepository processedVocabularyRepository,
            IValidationManager validationManager,
            IAreaHelper areaHelper,
            IVocabularyValueResolver vocabularyValueResolver,
            ITaxonRepository processedTaxonRepository,
            IVirtualHerbariumObservationVerbatimRepository virtualHerbariumObservationVerbatimRepository)
            : base(processedVocabularyRepository, validationManager, areaHelper, vocabularyValueResolver, processedTaxonRepository)
        {
            _virtualHerbariumObservationVerbatimRepository = virtualHerbariumObservationVerbatimRepository;
        }

        protected override async Task<IAsyncCursor<VirtualHerbariumObservationVerbatim>> GetAllObservationsByCursorAsync()
        {
            return await _virtualHerbariumObservationVerbatimRepository.GetAllByCursorAsync();
        }

        protected override async Task<long> GetTotalObservationsCountAsync()
        {
            return await _virtualHerbariumObservationVerbatimRepository.CountAllDocumentsAsync();
        }

        protected override Observation CreateProcessedObservation(VirtualHerbariumObservationVerbatim verbatimObservation, DataProvider dataProvider)
        {
            var processedObservation = GetObservationFactory(dataProvider).CreateProcessedObservation(verbatimObservation);
            _areaHelper.AddAreaDataToProcessedObservation(processedObservation);
            return processedObservation;
        }

        protected override void ValidateVerbatimTaxon(
            VirtualHerbariumObservationVerbatim verbatimObservation,
            HashSet<int> nonMatchingTaxonIds,
            HashSet<string> nonMatchingScientificNames)
        {
            nonMatchingTaxonIds.Add(verbatimObservation.DyntaxaId);
        }

        protected override void ValidateVerbatimData(VirtualHerbariumObservationVerbatim verbatimObservation, DwcaValidationRemarksBuilder validationRemarksBuilder)
        {
            validationRemarksBuilder.NrValidatedObservations++;

            if (!verbatimObservation.CoordinatePrecision.HasValue)
            {
                validationRemarksBuilder.NrMissingCoordinateUncertaintyInMeters++;
            }
        }

        protected override void UpdateTermDictionaryValueSummary(
            Observation processedObservation,
            VirtualHerbariumObservationVerbatim verbatimObservation,
            Dictionary<VocabularyId, Dictionary<VocabularyValue, int>> processedFieldValues,
            Dictionary<VocabularyId, Dictionary<VocabularyValue, HashSet<string>>> verbatimFieldValues)
        {
            // Virtual Herbarium doesn't contain any vocabulary fields.
        }

        private VirtualHerbariumObservationFactory GetObservationFactory(DataProvider dataProvider)
        {
            if (_virtualHerbariumObservationFactory == null)
            {
                _virtualHerbariumObservationFactory = new VirtualHerbariumObservationFactory(dataProvider, _taxonById);
            }

            return _virtualHerbariumObservationFactory;
        }
    }
}