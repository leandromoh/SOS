﻿using System.Collections.Generic;

namespace SOS.Lib.Models.Processed.DarwinCore
{
    /// <summary>
    /// This class contains fields not defined in Darwin Core.
    /// </summary>
    public class TaxonDynamicProperties
    {
        /// <summary>
        /// Dyntaxa taxon id.
        /// </summary>
        public int DyntaxaTaxonId { get; set; }

        /// <summary>
        /// Main parent Dyntaxa taxon id.
        /// </summary>
        public int? ParentDyntaxaTaxonId { get; set; }

        /// <summary>
        /// Secondary parents dyntaxa taxon ids.
        /// </summary>
        public IEnumerable<int> SecondaryParentDyntaxaTaxonIds { get; set; }

        /// <summary>
        /// Vernacular names.
        /// </summary>
        public IEnumerable<TaxonVernacularName> VernacularNames { get; set; }

        /// <summary>
        /// Action plan
        /// </summary>
        public string ActionPlan { get; set; }

        /// <summary>
        /// Part of bird directive?
        /// </summary>
        public bool? BirdDirective { get; set; }
        
        /// <summary>
        /// Radius of disturbance
        /// </summary>
        public int DisturbanceRadius { get; set; }

        /// <summary>
        /// part of Habitats directive article 2
        /// </summary>
        public bool? Natura2000HabitatsDirectiveArticle2 { get; set; }

        /// <summary>
        /// part of Habitats directive article 2
        /// </summary>
        public bool? Natura2000HabitatsDirectiveArticle4 { get; set; }

        /// <summary>
        /// part of Habitats directive article 2
        /// </summary>
        public bool? Natura2000HabitatsDirectiveArticle5 { get; set; }

        /// <summary>
        /// Organism group
        /// </summary>
        public string OrganismGroup { get; set; }

        /// <summary>
        /// True if taxon is protected by law
        /// </summary>
        public bool? ProtectedByLaw { get; set; }

        /// <summary>
        /// True if taxon is protected by law
        /// </summary>
        public string ProtectionLevel { get; set; }

        /// <summary>
        /// Redlist category
        /// </summary>
        public string RedlistCategory { get; set; }

        /// <summary>
        /// Do taxon occur in sweden 
        /// </summary>
        public string SwedishOccurrence { get; set; }

        /// <summary>
        /// Do taxon occur in sweden 
        /// </summary>
        public string SwedishHistory { get; set; }
    }
}