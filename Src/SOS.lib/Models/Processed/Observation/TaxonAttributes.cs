﻿using System.Collections.Generic;
using Nest;

namespace SOS.Lib.Models.Processed.Observation
{
    /// <summary>
    /// Taxon attributes.
    /// </summary>
    public class TaxonAttributes 
    {
        /// <summary>
        /// Indicates whether the species is the subject
        /// of an action plan ('åtgärdsprogram' in swedish).
        /// </summary>
        public string ActionPlan { get; set; }

        /// <summary>
        /// Disturbance radius.
        /// </summary>
        public int? DisturbanceRadius { get; set; }

        /// <summary>
        /// Taxon id value in Dyntaxa.
        /// </summary>
        public int DyntaxaTaxonId { get; set; }

        /// <summary>
        /// Natura 2000, Habitats directive article 2.
        /// </summary>
        public bool? Natura2000HabitatsDirectiveArticle2 { get; set; }

        /// <summary>
        /// Natura 2000, Habitats directive article 4.
        /// </summary>
        public bool? Natura2000HabitatsDirectiveArticle4 { get; set; }

        /// <summary>
        /// Natura 2000, Habitats directive article 5.
        /// </summary>
        public bool? Natura2000HabitatsDirectiveArticle5 { get; set; }

        /// <summary>
        /// Common name of the organism group that observed species
        /// belongs to. Classification of species groups is the same as
        /// used in latest 'Red List of Swedish Species'.
        /// </summary>
        public string OrganismGroup { get; set; }

        /// <summary>
        /// Parent Dyntaxa TaxonId.
        /// </summary>
        public int? ParentDyntaxaTaxonId { get; set; }

        /// <summary>
        /// Indicates whether the species 
        /// is protected by the law in Sweden.
        /// </summary>
        public bool? ProtectedByLaw { get; set; }

        /// <summary>
        /// Information about how protected information about a species is in Sweden.
        /// This is a value between 1 to 5.
        /// 1 indicates public access and 5 is the highest used security level.
        /// </summary>
        public VocabularyValue ProtectionLevel { get; set; }

        /// <summary>
        /// Redlist category for redlisted species. Possible redlist values
        /// are DD (Data Deficient), EX (Extinct),
        /// RE (Regionally Extinct), CR (Critically Endangered),
        /// EN (Endangered), VU (Vulnerable), NT (Near Threatened).
        /// Not redlisted species has no value in this property.
        /// </summary>
        public string RedlistCategory { get; set; }

        /// <summary>
        /// Systematic sort order.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// This property contains information about the species
        /// immigration history.
        /// </summary>
        public string SwedishHistory { get; set; }

        /// <summary>
        /// Information about the species occurrence in Sweden.
        /// For example information about if the species reproduce
        /// in sweden.
        /// </summary>
        public string SwedishOccurrence { get; set; }

        /// <summary>
        ///     Scientific synonym names.
        /// </summary>
        [Nested]
        public IEnumerable<TaxonSynonymName> Synonyms { get; set; }

        /// <summary>
        ///     Vernacular names.
        /// </summary>
        [Nested]
        public IEnumerable<TaxonVernacularName> VernacularNames { get; set; }
    }
}