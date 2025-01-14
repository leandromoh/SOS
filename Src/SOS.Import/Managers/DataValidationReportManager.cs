﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Import.Factories.Validation;
using SOS.Import.Factories.Validation.Interfaces;
using SOS.Import.Managers.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Models.DataValidation;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;

namespace SOS.Import.Managers
{
    public class DataValidationReportManager : IDataValidationReportManager
    {
        private readonly Dictionary<DataProviderType, IDataValidationReportFactory> _reportCreatorByType;

        public DataValidationReportManager(
            ClamPortalDataValidationReportFactory clamPortalDataValidationReportFactory,
            FishDataValidationReportFactory fishDataValidationReportFactory,
            DwcaDataValidationReportFactory dwcaDataValidationReportFactory,
            MvmDataValidationReportFactory mvmDataValidationReportFactory,
            KulDataValidationReportFactory kulDataValidationReportFactory,
            SersDataValidationReportFactory sersDataValidationReportFactory,
            NorsDataValidationReportFactory norsDataValidationReportFactory, 
            VirtualHerbariumValidationReportFactory virtualHerbariumDataValidationReportFactory)
        {
            if (clamPortalDataValidationReportFactory == null) throw new ArgumentNullException(nameof(clamPortalDataValidationReportFactory));
            if (fishDataValidationReportFactory == null) throw new ArgumentNullException(nameof(fishDataValidationReportFactory));
            if (dwcaDataValidationReportFactory == null) throw new ArgumentNullException(nameof(dwcaDataValidationReportFactory));
            if (mvmDataValidationReportFactory == null) throw new ArgumentNullException(nameof(mvmDataValidationReportFactory));
            if (kulDataValidationReportFactory == null) throw new ArgumentNullException(nameof(kulDataValidationReportFactory));
            if (sersDataValidationReportFactory == null) throw new ArgumentNullException(nameof(sersDataValidationReportFactory));
            if (norsDataValidationReportFactory == null) throw new ArgumentNullException(nameof(norsDataValidationReportFactory));
            if (virtualHerbariumDataValidationReportFactory == null) throw new ArgumentNullException(nameof(virtualHerbariumDataValidationReportFactory));

            _reportCreatorByType = new Dictionary<DataProviderType, IDataValidationReportFactory>
            {
                {DataProviderType.ClamPortalObservations, clamPortalDataValidationReportFactory},
                {DataProviderType.FishDataObservations, fishDataValidationReportFactory},
                {DataProviderType.MvmObservations, mvmDataValidationReportFactory},
                {DataProviderType.KULObservations, kulDataValidationReportFactory},
                {DataProviderType.SersObservations, sersDataValidationReportFactory},
                {DataProviderType.NorsObservations, norsDataValidationReportFactory},
                {DataProviderType.VirtualHerbariumObservations, virtualHerbariumDataValidationReportFactory},
                {DataProviderType.DwcA, dwcaDataValidationReportFactory},
                {DataProviderType.iNaturalistObservations, dwcaDataValidationReportFactory}
            };
        }

        public async Task<DataValidationReport<object, Observation>> CreateDataValidationReport(
            DataProvider dataProvider, 
            int maxNrObservationsToRead = 100000,
            int nrValidObservationsInReport = 10, 
            int nrInvalidObservationsInReport = 100)
        {
            _reportCreatorByType.TryGetValue(dataProvider.Type, out var reportCreator);
            if (reportCreator == null) throw new ArgumentException($"CreateValidationReport() - No support for data provider: {dataProvider}");

            var dataValidationSummary = await reportCreator.CreateDataValidationSummary(
                dataProvider,
                maxNrObservationsToRead,
                nrValidObservationsInReport,
                nrInvalidObservationsInReport);

            return dataValidationSummary;
        }
    }
}