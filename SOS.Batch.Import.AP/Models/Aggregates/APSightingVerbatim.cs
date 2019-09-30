﻿using System;
using System.Collections.Generic;

namespace SOS.Batch.Import.AP.Models.Aggregates
{
    /// <summary>
    /// Sighting object
    /// </summary>
    public class APSightingVerbatim : Interfaces.IEntity<int>
    {
        /// <summary>
        /// Id of activity
        /// </summary>
        public Metadata Activity { get; set; }

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
        public Metadata Gender { get; set; }

        /// <summary>
        /// Hidden by provider date
        /// </summary>
        public DateTime? HiddenByProvider { get; set; }

        /// <summary>
        /// Id of sighting
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Taxon length
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Not present flag
        /// </summary>
        public bool NotPresent { get; set; }

        /// <summary>
        /// Not recovered flag
        /// </summary>
        public bool NotRecovered { get; set; }

        /// <summary>
        /// Protected by system flag
        /// </summary>
        public bool ProtectedBySystem { get; set; }

        /// <summary>
        /// Projects
        /// </summary>
        public IEnumerable<Project> Projects { get; set; }

        /// <summary>
        /// Number of taxa found
        /// </summary>
        public int? Quantity { get; set; }
        
        /// <summary>
        /// Id of site
        /// </summary>
        public Site Site { get; set; }

        /// <summary>
        /// Taxon stage id
        /// </summary>
        public Metadata Stage { get; set; }

        /// <summary>
        /// Sif´ghting start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Sighting start time
        /// </summary>
        public TimeSpan? StartTime { get; set; }
        
        /// <summary>
        /// Taxon
        /// </summary>
        public Taxon Taxon { get; set; }

        /// <summary>
        /// Id of unit
        /// </summary>
        public Metadata Unit { get; set; }

        /// <summary>
        /// Un spontaneous flag
        /// </summary>
        public bool Unspontaneous { get; set; }

        /// <summary>
        /// Unsecure determination
        /// </summary>
        public bool UnsureDetermination { get; set; }

        /// <summary>
        /// Taxon weight
        /// </summary>
        public int? Weight { get; set; }
    }
}