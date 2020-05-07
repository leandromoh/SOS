﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nest;
using NetTopologySuite.Geometries;
using SOS.Lib.Enums;
using SOS.Lib.Enums.FieldMappingValues;
using SOS.Lib.Extensions;
using SOS.Lib.Helpers;
using SOS.Lib.Models.DarwinCore.Vocabulary;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Verbatim.Mvm;

namespace SOS.Process.Processors.Mvm
{
    public class MvmObservationFactory
    {
        private const int DefaultCoordinateUncertaintyInMeters = 500;
        private readonly IDictionary<int, ProcessedTaxon> _taxa;

        public MvmObservationFactory(IDictionary<int, ProcessedTaxon> taxa)
        {
            _taxa = taxa ?? throw new ArgumentNullException(nameof(taxa));
        }

        /// <summary>
        /// Cast multiple clam observations to ProcessedObservation
        /// </summary>
        /// <param name="verbatims"></param>
        /// <returns></returns>
        public IEnumerable<ProcessedObservation> CreateProcessedObservations(IEnumerable<MvmObservationVerbatim> verbatims)
        {
            return verbatims.Select(CreateProcessedObservation);
        }

        /// <summary>
        /// Cast MVM observation verbatim to ProcessedObservation
        /// </summary>
        /// <param name="verbatim"></param>
        /// <returns></returns>
        public ProcessedObservation CreateProcessedObservation(MvmObservationVerbatim verbatim)
        {
            Point wgs84Point = null;
            if (verbatim.DecimalLongitude > 0 && verbatim.DecimalLatitude > 0)
            {
                wgs84Point = new Point(verbatim.DecimalLongitude, verbatim.DecimalLatitude) { SRID = (int)CoordinateSys.WGS84 };
            }

            _taxa.TryGetValue(verbatim.DyntaxaTaxonId, out var taxon);

            var obs = new ProcessedObservation(ObservationProvider.MVM)
            {
                BasisOfRecord = new ProcessedFieldMapValue { Id = (int)BasisOfRecordId.HumanObservation },
                DatasetId = $"urn:lsid:swedishlifewatch.se:dataprovider:{ObservationProvider.MVM.ToString()}",
                DatasetName = "MVM",
                Event = new ProcessedEvent
                {
                    EndDate = verbatim.End.ToUniversalTime(),
                    StartDate = verbatim.Start.ToUniversalTime(),
                    VerbatimEventDate = DwcFormatter.CreateDateIntervalString(verbatim.Start, verbatim.End)
                },
                Identification = new ProcessedIdentification
                {
                    Validated = true,
                    UncertainDetermination = false
                },
                Location = new ProcessedLocation
                {
                    CoordinateUncertaintyInMeters = verbatim.CoordinateUncertaintyInMeters ?? DefaultCoordinateUncertaintyInMeters,
                    CountryCode = CountryCode.Sweden,
                    DecimalLatitude = verbatim.DecimalLatitude,
                    DecimalLongitude = verbatim.DecimalLongitude,
                    GeodeticDatum = GeodeticDatum.Wgs84,
                    Continent = new ProcessedFieldMapValue { Id = (int)ContinentId.Europe },
                    Country = new ProcessedFieldMapValue { Id = (int)CountryId.Sweden },
                    Locality = verbatim.Locality,
                    Point = (PointGeoShape)wgs84Point?.ToGeoShape(),
                    PointLocation = wgs84Point?.ToGeoLocation(),
                    PointWithBuffer = (PolygonGeoShape)wgs84Point?.ToCircle(verbatim.CoordinateUncertaintyInMeters)?.ToGeoShape(),
                    VerbatimLatitude = verbatim.DecimalLatitude,
                    VerbatimLongitude = verbatim.DecimalLongitude
                },
                Modified = DateTime.Parse(verbatim.Modified).ToUniversalTime(),
                Occurrence = new ProcessedOccurrence
                {
                    CatalogNumber = GetCatalogNumber(verbatim.OccurrenceId),
                    OccurrenceId = verbatim.OccurrenceId,
                    IsNaturalOccurrence = true,
                    IsNeverFoundObservation = GetIsNeverFoundObservation(verbatim.DyntaxaTaxonId),
                    IsNotRediscoveredObservation = false,
                    IsPositiveObservation = GetIsPositiveObservation(verbatim.DyntaxaTaxonId),
                    RecordedBy = verbatim.RecordedBy,
                    OccurrenceStatus = GetOccurrenceStatusId(verbatim.DyntaxaTaxonId)
                },
                OwnerInstitutionCode = verbatim.Owner,
                ProtectionLevel = GetProtectionLevel(),
                ReportedBy = verbatim.ReportedBy,
                ReportedDate = verbatim.Start,
                Taxon = taxon
            };

            return obs;
        }

        /// <summary>
        /// Creates occurrence id.
        /// </summary>
        /// <returns>The Catalog Number.</returns>
        private string GetCatalogNumber(string occurrenceId)
        {
            var pos = occurrenceId.LastIndexOf(":", StringComparison.Ordinal);
            return occurrenceId.Substring(pos + 1);
        }

        /// <summary>
        /// Gets the occurrence status. Set to Present if DyntaxaTaxonId from provider is greater than 0 and Absent if DyntaxaTaxonId is 0
        /// </summary>
        private ProcessedFieldMapValue GetOccurrenceStatusId(int dyntaxaTaxonId)
        {
            if (dyntaxaTaxonId == 0)
            {
                return new ProcessedFieldMapValue { Id = (int)OccurrenceStatusId.Absent };
            }

            return new ProcessedFieldMapValue { Id = (int)OccurrenceStatusId.Present };
        }

        /// <summary>
        /// An integer value corresponding to the Enum of the Main field of the SpeciesFact FactorId 761.
        /// By default the value is 1. If the taxon is subordinate to the taxon category Species it is nessecary
        /// to check the Species Fact values of parent taxa.
        /// If the value is greater than 1 for any parent then the value should equal to the max value among parents.
        /// </summary>
        /// <returns></returns>
        private int GetProtectionLevel()
        {
            return 1;
        }

        /// <summary>
        /// Set to False if DyntaxaTaxonId from provider is greater than 0 and True if DyntaxaTaxonId is 0.
        /// </summary>
        private bool GetIsNeverFoundObservation(int dyntaxaTaxonId)
        {
            return dyntaxaTaxonId == 0;
        }

        /// <summary>
        /// Set to True if DyntaxaTaxonId from provider is greater than 0 and False if DyntaxaTaxonId is 0.
        /// </summary>
        private bool GetIsPositiveObservation(int dyntaxaTaxonId)
        {
            return dyntaxaTaxonId != 0;
        }
    }
}