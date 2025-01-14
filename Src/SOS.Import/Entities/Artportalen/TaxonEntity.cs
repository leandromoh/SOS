﻿namespace SOS.Import.Entities.Artportalen
{
    /// <summary>
    ///     Taxon object
    /// </summary>
    public class TaxonEntity
    {
        /// <summary>
        ///     Taxon category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        ///     Id of taxon
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     Taxon swedish name
        /// </summary>
        public string ScientificName { get; set; }

        /// <summary>
        ///     Taxon scientific name
        /// </summary>
        public string SwedishName { get; set; }
    }
}