﻿using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Verbatim.DarwinCore;

namespace SOS.Lib.Extensions
{
    /// <summary>
    ///     Extensions for Darwin Core
    /// </summary>
    public static class DarwinCoreMeasurementOrFactsExtensions
    {
        public static ProcessedExtendedMeasurementOrFact ToProcessedExtendedMeasurementOrFact(this DwcMeasurementOrFact dwcMeasurementOrFact)
        {
            return new ProcessedExtendedMeasurementOrFact
            {
                MeasurementID = dwcMeasurementOrFact.MeasurementID,
                MeasurementType = dwcMeasurementOrFact.MeasurementType,
                MeasurementValue = dwcMeasurementOrFact.MeasurementValue,
                MeasurementAccuracy = dwcMeasurementOrFact.MeasurementAccuracy,
                MeasurementUnit = dwcMeasurementOrFact.MeasurementUnit,
                MeasurementDeterminedDate = dwcMeasurementOrFact.MeasurementDeterminedDate,
                MeasurementDeterminedBy = dwcMeasurementOrFact.MeasurementDeterminedBy,
                MeasurementMethod = dwcMeasurementOrFact.MeasurementMethod,
                MeasurementRemarks = dwcMeasurementOrFact.MeasurementRemarks
            };
        }

        public static ProcessedExtendedMeasurementOrFact ToProcessedExtendedMeasurementOrFact(this DwcExtendedMeasurementOrFact dwcEmof)
        {
            return new ProcessedExtendedMeasurementOrFact()
            {
                MeasurementID = dwcEmof.MeasurementID,
                MeasurementType = dwcEmof.MeasurementType,
                MeasurementTypeID = dwcEmof.MeasurementTypeID,
                MeasurementValue = dwcEmof.MeasurementValue,
                MeasurementValueID = dwcEmof.MeasurementValueID,
                MeasurementAccuracy = dwcEmof.MeasurementAccuracy,
                MeasurementUnit = dwcEmof.MeasurementUnit,
                MeasurementUnitID = dwcEmof.MeasurementUnitID,
                MeasurementDeterminedDate = dwcEmof.MeasurementDeterminedDate,
                MeasurementDeterminedBy = dwcEmof.MeasurementDeterminedBy,
                MeasurementMethod = dwcEmof.MeasurementMethod,
                MeasurementRemarks = dwcEmof.MeasurementRemarks,
                OccurrenceID = dwcEmof.OccurrenceID
            };
        }

    }
}