﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using SOS.Lib.Enums;

namespace SOS.Lib.Models.Statistics
{
    public class UserStatisticsItem
    {
        /// <summary>
        /// UserId.
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Number of species (taxa) the user has found.
        /// </summary>
        public int SpeciesCount { get; set; }

        /// <summary>
        /// Number of observations.
        /// </summary>
        public int ObservationCount { get; set; }

        /// <summary>
        /// Number of species (taxa) per area.
        /// </summary>
        public List<AreaSpeciesCount> AreaCounts { get; set; }
    }

    public class AreaSpeciesCount
    {
        public string FeatureId { get; set; }
        public int SpeciesCount { get; set; }

        public AreaSpeciesCount() {}

        public AreaSpeciesCount(string featureId, int speciesCount)
        {
            FeatureId = featureId;
            SpeciesCount = speciesCount;
        }
    }
}