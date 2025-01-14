﻿using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using SOS.Lib.Enums;
using SOS.Lib.Helpers;
using SOS.Lib.Models.Search;

namespace SOS.Lib.Extensions
{
    /// <summary>
    /// Search filter extensions
    /// </summary>
    public static class SearchFilterExtensions
    {
        public static void PopulateExportOutputFields(this SearchFilter filter, OutputFieldSet outputFieldSet = OutputFieldSet.Minimum)
        {
            var outputFields = ObservationPropertyFieldDescriptionHelper.OutputFieldsByFieldSet[outputFieldSet];
            filter.OutputFields = outputFields;
        }

        /// <summary>
        /// Populate output fields based on property set
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="outputFieldSet"></param>
        public static void PopulateOutputFields(this SearchFilter filter, OutputFieldSet? outputFieldSet)
        {
            if (filter.OutputFields?.Any() == true && outputFieldSet == null) return;
            const OutputFieldSet defaultFieldSet = OutputFieldSet.Minimum;
            var fieldSet = outputFieldSet == null ? defaultFieldSet : (OutputFieldSet)outputFieldSet;
            if (fieldSet == OutputFieldSet.AllWithKnownValues || fieldSet == OutputFieldSet.All)
            {
                filter.OutputFields = null;
                return;
            }

            var outputFields = new List<string>
            {
                "DatasetName",
                "Identification.Validated",
                "Identification.UncertainIdentification",
                "Location.County",
                "Location.Municipality",
                "Location.DecimalLongitude",
                "Location.DecimalLatitude",
                "Location.CoordinateUncertaintyInMeters",
                "Occurrence.OccurrenceStatus",
                "Occurrence.ReportedBy",
                "Occurrence.RecordedBy",
                "Occurrence.IndividualCount",
                "Occurrence.OrganismQuantity",
                "Occurrence.OrganismQuantityInt",
                "Occurrence.OrganismQuantityUnit",
                "Taxon.Id",
                "Taxon.ScientificName",
                "Taxon.VernacularName",
                "Taxon.Attributes.OrganismGroup",
            };

            if (fieldSet == OutputFieldSet.Extended)
            {
                outputFields.AddRange(new[]
                {
                    "CollectionCode",
                    "InstitutionCode",
                    "OwnerInstitutionCode",
                    "BasisOfRecord",
                    "DataProviderId",
                    "RightsHolder",
                    "Modified",
                    "Projects.Id",
                    "Projects.Name",
                    "Projects.Owner",
                    "Projects.ProjectParameters",
                    "MeasurementOrFacts.MeasurementID",
                    "MeasurementOrFacts.MeasurementType",
                    "MeasurementOrFacts.MeasurementValue",
                    "MeasurementOrFacts.MeasurementUnit",
                    "Event.Habitat",
                    "Event.EventRemarks",
                    "Event.SamplingEffort",
                    "Event.SamplingProtocol",
                    "Event.SampleSizeUnit",
                    "Event.SampleSizeValue",
                    "Event.MeasurementOrFacts.MeasurementID",
                    "Event.MeasurementOrFacts.MeasurementType",
                    "Event.MeasurementOrFacts.MeasurementValue",
                    "Event.MeasurementOrFacts.MeasurementUnit",
                    "Location.Locality",
                    "Location.Province",
                    "Location.Parish",
                    "Location.GeodeticDatum",
                    "Location.LocationId",
                    "Occurrence.Url",
                    "Occurrence.AssociatedMedia",
                    "Occurrence.OccurrenceRemarks",
                    "Occurrence.Activity",
                    "Occurrence.Behavior",
                    "Occurrence.LifeStage",
                    "Occurrence.ReproductiveCondition",
                    "Occurrence.Sex",
                    "Occurrence.Biotope",
                    "Occurrence.BiotopeDescription",
                    "Occurrence.ProtectionLevel",
                    "Occurrence.IsNeverFoundObservation",
                    "Occurrence.IsNotRediscoveredObservation",
                    "Occurrence.IsNaturalOccurrence",
                    "Occurrence.IsPositiveObservation",
                    "Occurrence.Substrate.Name",
                    "Occurrence.Length",
                    "Occurrence.Weight",
                    "Identification.ValidationStatus",
                    "Identification.ConfirmedBy",
                    "Identification.IdentifiedBy",
                    "Identification.VerifiedBy",
                    "Identification.DeterminationMethod",
                    "Taxon.Kingdom",
                    "Taxon.Phylum",
                    "Taxon.Class",
                    "Taxon.Order",
                    "Taxon.Family",
                    "Taxon.Genus",
                    "Taxon.TaxonId",
                    "Taxon.Attributes.ProtectionLevel",
                    "Taxon.Attributes.RedlistCategory",
                    "Taxon.Attributes.ProtectedByLaw"
                });
            }

            // Order fields by name
            outputFields = outputFields.OrderBy(s => s).ToList();

            // Make sure some selected fields occurs first
            outputFields.InsertRange(0, new[]
            {
                "Occurrence.OccurrenceId",
                "Event.StartDate",
                "Event.EndDate"
            });

            if (filter.OutputFields?.Any() ?? false)
            {
                outputFields.AddRange(filter.OutputFields.Where(of => !outputFields.Contains(of, StringComparer.CurrentCultureIgnoreCase)));
            }
           
            filter.OutputFields = outputFields;
        }
    }
}
