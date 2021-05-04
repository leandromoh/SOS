﻿using System;
using System.Collections.Generic;
using Nest;
using SOS.Lib.Models.Interfaces;

namespace SOS.Lib.Models.Shared
{
    /// <summary>
    ///     Taxon list.
    /// </summary>
    public class TaxonList : IEntity<int>
    {
        /// <summary>
        /// The Id of the taxon list.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The Id in Taxon list service.
        /// </summary>
        public int TaxonListServiceId { get; set; }

        /// <summary>
        ///     The names of the taxon list
        /// </summary>
        public IEnumerable<VocabularyValueTranslation> Names { get; set; }

        /// <summary>
        /// The taxa in this taxon list.
        /// </summary>
        public IEnumerable<TaxonListTaxonInformation> Taxa { get; set; }
    }
}