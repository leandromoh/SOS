﻿using System.Collections.Generic;
using Nest;
using SOS.Lib.Models.Shared;
using SOS.Lib.Swagger;

namespace SOS.Lib.Models.Processed.Observation
{
    /// <summary>
    ///     Observation information specific for Artportalen.
    /// </summary>
    public class ArtportalenInternal
    {
        /// <summary>
        /// Bird validation areas.
        /// </summary>
        public IEnumerable<string> BirdValidationAreaIds { get; set; }

        /// <summary>
        /// Year of confirmation.
        /// </summary>
        public int? ConfirmationYear { get; set; }

        /// <summary>
        /// Data source id
        /// </summary>
        public int? DatasourceId { get; set; }

        /// <summary>
        /// Year of determination.
        /// </summary>
        public int? DeterminationYear { get; set; }

        /// <summary>
        ///     Ids of Species Facts connected to Taxon
        /// </summary>
        public IEnumerable<int> SpeciesFactsIds { get; set; }

        /// <summary>
        ///     Id of SightingSpeciesCollectionItem in Artportalen.
        /// </summary>
        public int? SightingSpeciesCollectionItemId { get; set; }

        /// <summary>
        ///     Has Triggered Validation Rules
        /// </summary>
        public bool HasTriggeredValidationRules { get; set; }

        /// <summary>
        ///     Has any Triggered Validation Rule with Warning
        /// </summary>
        public bool HasAnyTriggeredValidationRuleWithWarning { get; set; }

        /// <summary>
        ///     ExternalId of Site in Artportalen.
        /// </summary>
        public string LocationExternalId { get; set; }

        /// <summary>
        ///     Note of Interest.
        /// </summary>
        public bool NoteOfInterest { get; set; }

        /// <summary>
        ///     HasUserComments
        /// </summary>
        public bool HasUserComments { get; set; }

        /// <summary>
        /// Sighting Id.
        /// </summary>
        public int SightingId { get; set; }

        /// <summary>
        ///     Sighting type.
        /// </summary>
        public int SightingTypeId { get; set; }

        /// <summary>
        ///     Sighting type search group id.
        /// </summary>
        public int SightingTypeSearchGroupId { get; set; }

        /// <summary>
        ///     Id of sightings RegionalSightingState
        /// </summary>
        public int? RegionalSightingStateId { get; set; }

        /// <summary>
        ///     Id of publishing types.
        /// </summary>
        public IEnumerable<int> SightingPublishTypeIds { get; set; }
        
        /// <summary>
        ///     Internal field used for searches by Artportalen, contains extra user information.
        /// </summary>
        [Nested]
        public IEnumerable<UserInternal> OccurrenceRecordedByInternal { get; set; }

        /// <summary>
        /// Info about users verifying the observation
        /// </summary>
        [Nested]
        public IEnumerable<UserInternal> OccurrenceVerifiedByInternal { get; set; }

        /// <summary>
        ///     The original presentation name for ParisRegion from data provider.
        /// </summary>
        public string LocationPresentationNameParishRegion { get; set; }

        /// <summary>
        ///     The parent location id of the current location, this is used by Artportalen for bird locations that
        ///     have one main location and several sublocation.
        /// </summary>
        [SwaggerExclude]
        public int? ParentLocationId { get; set; }

        /// <summary>
        ///     Name of parent location, if any.
        /// </summary>
        public string ParentLocality { get; set; }

        /// <summary>
        ///     User id of the person that reported the species observation.
        /// </summary>
        public int? ReportedByUserId { get; set; }

        /// <summary>
        ///     User Service id of the person that reported the species observation.
        /// </summary>
        [SwaggerExclude]
        public int? ReportedByUserServiceUserId { get; set; }
        
        /// <summary>
        ///     Alias for the reporter, internal use only.
        /// </summary>
        [SwaggerExclude]
        public string ReportedByUserAlias { get; set; }

        /// <summary>
        /// True if sighting was incremental harvested.
        /// </summary>
        public bool IncrementalHarvested { get; set; }

        /// <summary>
        ///  Sighting barcode url
        /// </summary>
        public string SightingBarcodeURL { get; set; }
    }
}