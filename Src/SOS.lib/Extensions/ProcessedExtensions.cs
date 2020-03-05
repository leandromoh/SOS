﻿using System.Collections.Generic;
using System.Linq;
using SOS.Lib.Constants;
using  SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Processed.Sighting;

namespace SOS.Lib.Extensions
{
    /// <summary>
    /// Extensions for Darwin Core
    /// </summary>
    public static class ProcessedExtensions
    {
        #region Event
        public static DarwinCoreEvent ToDarwinCore(this ProcessedEvent source)
        {
            if (source == null)
            {
                return null;
            }

            var biotope = source.BiotopeId?.Value;
            return new DarwinCoreEvent
            {
                EventDate = $"{source.StartDate?.ToString("s")}Z" ?? "",
                EventTime = source.StartDate?.ToUniversalTime().ToString("HH':'mm':'ss''K") ?? "",
                Habitat = (biotope != null
                    ? $"{biotope}{(string.IsNullOrEmpty(source.BiotopeDescription) ? "" : " # ")}{source.BiotopeDescription}"
                    : source.BiotopeDescription).WithMaxLength(255),
                SamplingProtocol = source.SamplingProtocol,
                VerbatimEventDate =
                    $"{(source.StartDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")}{(source.StartDate.HasValue && source.EndDate.HasValue ? "-" : "")}{(source.EndDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")}"

            };
        }
        #endregion Event

        #region Identification
        public static DarwinCoreIdentification ToDarwinCore(this ProcessedIdentification source)
        {
            if (source == null)
            {
                return null;
            }

            return new DarwinCoreIdentification
            {
                IdentificationVerificationStatus = source.ValidationStatusId?.Value,
                IdentifiedBy = source.IdentifiedBy,
                IdentificationReferences = source.References,
                IdentificationRemarks = source.Remarks,
                IdentificationID = source.Id,
                IdentificationQualifier = source.Qualifier,
                DateIdentified = $"{source.DateIdentified.ToString("s")}Z" ?? "",
                TypeStatus = source.TypeStatus
            };
        }
        #endregion Identification

        #region Location
        public static DarwinCoreLocation ToDarwinCore(this ProcessedLocation source)
        {
            if (source == null)
            {
                return null;
            }

            // todo - initialize the Value property for field mapped types.
            return new DarwinCoreLocation
            {
                Continent = source.Continent,
                CoordinatePrecision = source.CoordinatePrecision?.ToString(),
                CoordinateUncertaintyInMeters = source.CoordinateUncertaintyInMeters,
                Country = source.Country,
                CountryCode = source.CountryCode,
                County = source.CountyId?.Value,
                DecimalLatitude = source.DecimalLatitude,
                DecimalLongitude = source.DecimalLongitude,
                FootprintSRS = source.FootprintSRS,
                FootprintSpatialFit = source.FootprintSpatialFit,
                FootprintWKT = source.FootprintWKT,
                GeodeticDatum = source.GeodeticDatum,
                GeoreferenceProtocol = source.GeoreferenceProtocol,
                GeoreferenceRemarks = source.GeoreferenceRemarks,
                GeoreferenceSources = source.GeoreferenceSources,
                GeoreferenceVerificationStatus = source.GeoreferenceVerificationStatus,
                GeoreferencedBy = source.GeoreferencedBy,
                GeoreferencedDate = source.GeoreferencedDate,
                HigherGeography = source.HigherGeography,
                HigherGeographyID = source.HigherGeographyID,
                Island = source.Island,
                IslandGroup = source.IslandGroup,
                Locality = source.Locality,
                LocationAccordingTo = source.LocationAccordingTo,
                LocationID = source.Id,
                LocationRemarks = source.Remarks,
                MaximumDepthInMeters = source.MaximumDepthInMeters?.ToString(),
                MaximumDistanceAboveSurfaceInMeters = source.MaximumDistanceAboveSurfaceInMeters?.ToString(),
                MaximumElevationInMeters = source.MaximumElevationInMeters?.ToString(),
                MinimumDepthInMeters = source.MinimumDepthInMeters?.ToString(),
                MinimumDistanceAboveSurfaceInMeters = source.MinimumDistanceAboveSurfaceInMeters?.ToString(),
                MinimumElevationInMeters = source.MinimumElevationInMeters?.ToString(),
                Municipality = source.MunicipalityId?.Value,
                PointRadiusSpatialFit = source.PointRadiusSpatialFit,
                StateProvince = source.ProvinceId?.Value,
                VerbatimCoordinates = null,
                VerbatimCoordinateSystem = source.VerbatimCoordinateSystem,
                VerbatimDepth = source.VerbatimDepth?.ToString(),
                VerbatimElevation = source.VerbatimElevation?.ToString(),
                VerbatimLatitude = source.VerbatimLatitude.ToString("0.0###########"),
                VerbatimLocality = source.VerbatimLocality,
                VerbatimLongitude = source.VerbatimLongitude.ToString("0.0###########"),
                VerbatimSRS = source.VerbatimSRS,
                WaterBody = source.WaterBody
            };
        }
        #endregion Location

        #region Occurrence
        public static DarwinCoreOccurrence ToDarwinCore(this ProcessedOccurrence source)
        {
            if (source == null)
            {
                return null;
            }

            return new DarwinCoreOccurrence
            {
                AssociatedMedia = source.AssociatedMedia,
                AssociatedOccurrences = source.AssociatedOccurrences,
                AssociatedReferences = source.AssociatedReferences,
                AssociatedSequences = source.AssociatedSequences,
                AssociatedTaxa = source.AssociatedTaxa,
                Behavior = source.ActivityId?.Value,
                CatalogNumber = source.CatalogNumber,
                Disposition = source.Disposition,
                EstablishmentMeans = source.EstablishmentMeans,
                IndividualCount = source.IndividualCount,
                IndividualID = source.IndividualID,
                LifeStage = source.LifeStageId?.Value,
                OccurrenceID = source.Id,
                OccurrenceRemarks = source.Remarks,
                OccurrenceStatus = source.Status,
                OrganismQuantity = source.OrganismQuantity?.ToString(),
                OrganismQuantityType = source.OrganismQuantity.HasValue ? source.OrganismQuantityUnitId?.Value ?? "Individuals" : null,
                OtherCatalogNumbers = source.OtherCatalogNumbers,
                RecordedBy = source.RecordedBy,
                ReproductiveCondition = source.ActivityId?.Value,
                Sex = source.GenderId?.Value
            };
        }
        #endregion Occurrence

        #region Sighting
        /// <summary>
        /// Cast processed sighting object to Darwin Core
        /// </summary>
        /// <param name="processedSighting"></param>
        /// <returns></returns>
        public static DarwinCore ToDarwinCore(this ProcessedSighting processedSighting)
        {
            if (processedSighting == null)
            {
                return null;
            }

            return new DarwinCore
            {
                AccessRights = processedSighting.AccessRights,
                BasisOfRecord = processedSighting.BasisOfRecord,
                BibliographicCitation = processedSighting.BasisOfRecord,
                CollectionCode = processedSighting.CollectionCode,
                CollectionID = processedSighting.CollectionId,
                DataGeneralizations = processedSighting.DataGeneralizations,
                DatasetID = processedSighting.DatasetId,
                DatasetName = processedSighting.DatasetName,
                Event = processedSighting.Event?.ToDarwinCore(),
                Identification = processedSighting.Identification?.ToDarwinCore(),
                InformationWithheld = processedSighting.InformationWithheld,
                InstitutionCode = processedSighting.OrganizationId?.Value,
                InstitutionID = processedSighting.OrganizationId == null ? null : $"urn:lsid:artdata.slu.se:organization:{processedSighting.OrganizationId.Id}",
                Language = processedSighting.Language,
                Location = processedSighting.Location?.ToDarwinCore(),
                MeasurementOrFact = null,
                Modified = processedSighting.Modified,
                Occurrence = processedSighting.Occurrence?.ToDarwinCore(),
                OwnerInstitutionCode = processedSighting.OwnerInstitutionCode,
                References = processedSighting.References,
                Rights = processedSighting.Rights,
                RightsHolder = processedSighting.RightsHolder,
                Taxon = processedSighting.Taxon.ToDarwinCore(),
                Type = processedSighting.Type
            };
        }

        /// <summary>
        ///  Cast processed Darwin Core objects to Darwin Core 
        /// </summary>
        /// <param name="processedSightings"></param>
        /// <returns></returns>
        public static IEnumerable<DarwinCore> ToDarwinCore(this IEnumerable<ProcessedSighting> processedSightings)
        {
            return processedSightings?.Select(m => m.ToDarwinCore());
        }
        #endregion Sighting

        #region Taxon
        /// <summary>
        /// Cats processed taxon to darwin core
        /// </summary>
        /// <param name="taxon"></param>
        /// <returns></returns>
        public static DarwinCoreTaxon ToDarwinCore(this ProcessedTaxon taxon)
        {
            return new DarwinCoreTaxon
            {
                AcceptedNameUsage = taxon.AcceptedNameUsage,
                AcceptedNameUsageID = taxon.AcceptedNameUsageID,
                Class = taxon.Class,
                Family = taxon.Family,
                Genus = taxon.Genus,
                HigherClassification = taxon.HigherClassification,
                InfraspecificEpithet = taxon.InfraspecificEpithet,
                Kingdom = taxon.Kingdom,
                NameAccordingTo = taxon.NameAccordingTo,
                NameAccordingToID = taxon.NameAccordingToID,
                NamePublishedIn = taxon.NamePublishedIn,
                NamePublishedInID = taxon.NamePublishedInId,
                NamePublishedInYear = taxon.NamePublishedInYear,
                NomenclaturalCode = taxon.NomenclaturalCode,
                NomenclaturalStatus = taxon.NomenclaturalStatus,
                Order = taxon.Order,
                OriginalNameUsage = taxon.OriginalNameUsage,
                OriginalNameUsageID = taxon.OriginalNameUsageId,
                ParentNameUsage = taxon.ParentNameUsage,
                ParentNameUsageID = taxon.ParentNameUsageId,
                Phylum = taxon.Phylum,
                ScientificName = taxon.ScientificName,
                ScientificNameAuthorship = taxon.ScientificNameAuthorship,
                ScientificNameID = taxon.ScientificNameId,
                SpecificEpithet = taxon.SpecificEpithet,
                Subgenus = taxon.Subgenus,
                TaxonConceptID = taxon.TaxonConceptId,
                TaxonID = taxon.TaxonId,
                VernacularName = taxon.VernacularName,
                TaxonRemarks = taxon.TaxonRemarks,
                TaxonRank = taxon.TaxonRank,
                TaxonomicStatus = taxon.TaxonomicStatus,
                VerbatimTaxonRank = taxon.VerbatimTaxonRank
            };
        }
        #endregion Taxon
    }
}
