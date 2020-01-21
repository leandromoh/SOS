﻿using System;

namespace SOS.Import.Entities
{
    /// <summary>
    /// Sighting object
    /// </summary>
    public class SightingEntity
    {
        /// <summary>
        /// Id of activity
        /// </summary>
        public int? ActivityId { get; set; }

        /// <summary>
        /// Id of biotope
        /// </summary>
        public int? BiptopeId { get; set; }

        /// <summary>
        /// Description of biotope
        /// </summary>
        public string BiptopeDescription { get; set; }

        /// <summary>
        /// Id of collection
        /// </summary>
        public string CollectionID { get; set; }

        /// <summary>
        /// SightingCommentPublic comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Id of controlling organisation
        /// </summary>
        public int? ControlingOrganisationId { get; set; }

        /// <summary>
        /// Sigthing end data
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Sighting end time
        /// </summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Taxon gender id
        /// </summary>
        public int? GenderId { get; set; }

        /// <summary>
        /// Has sighting images
        /// </summary>
        public bool HasImages { get; set; }
        
        /// <summary>
        /// Hidden by provider date
        /// </summary>
        public DateTime? HiddenByProvider { get; set; }

        /// <summary>
        /// Id of sighting
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// SightingSpeciesCollectionItem label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Taxon length
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Max depth
        /// </summary>
        public int? MaxDepth { get; set; }

        /// <summary>
        /// Max height
        /// </summary>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Min depth
        /// </summary>
        public int? MinDepth { get; set; }

        /// <summary>
        /// Min height
        /// </summary>
        public int? MinHeight { get; set; }
       
        /// <summary>
        /// Not present flag
        /// </summary>
        public bool NotPresent { get; set; }

        /// <summary>
        /// Not recovered flag
        /// </summary>
        public bool NotRecovered { get; set; }

        /// <summary>
        /// Migrate obs id
        /// </summary>
        public int? MigrateSightingObsId { get; set; }

        /// <summary>
        /// Migrate Portal id
        /// </summary>
        public int? MigrateSightingPortalId { get; set; }

        /// <summary>
        /// Owner organization
        /// </summary>
        public int? OwnerOrganizationId { get; set; }

        /// <summary>
        /// Protected by system flag
        /// </summary>
        public bool ProtectedBySystem { get; set; }

        /// <summary>
        /// Number of taxa found
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// Quality of substrate
        /// </summary>
        public int? QuantityOfSubstrate { get; set; }
        
        /// <summary>
        /// Date sighting was reported
        /// </summary>
        public DateTime? RegisterDate { get; set; }

        /// <summary>
        /// Rights holder
        /// </summary>
        public string RightsHolder { get; set; }

        /// <summary>
        /// Id of site
        /// </summary>
        public int? SiteId { get; set; }

        /// <summary>
        /// Taxon stage id
        /// </summary>
        public int? StageId { get; set; }

        /// <summary>
        /// Sif´ghting start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Sighting start time
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// Id of substrate
        /// </summary>
        public int? SubstrateId { get; set; }

        /// <summary>
        /// Description of substrate
        /// </summary>
        public string SubstrateDescription { get; set; }

        /// <summary>
        /// Description of substrate species
        /// </summary>
        public string SubstrateSpeciesDescription { get; set; }

        /// <summary>
        /// Substrate taxon id
        /// </summary>
        public int? SubstrateSpeciesId { get; set; }

        /// <summary>
        /// Id of taxon
        /// </summary>
        public int? TaxonId { get; set; }

        /// <summary>
        /// Id of unit
        /// </summary>
        public int? UnitId { get; set; }

        /// <summary>
        /// Un spontaneous flag
        /// </summary>
        public bool Unspontaneous { get; set; }

        /// <summary>
        /// Unsecure determination
        /// </summary>
        public bool UnsureDetermination { get; set; }

        /// <summary>
        /// SightingBarcode url
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Validation status id
        /// </summary>
        public int ValidationStatusId { get; set; }

        /// <summary>
        /// Taxon weight
        /// </summary>
        public int? Weight { get; set; }
    }
}