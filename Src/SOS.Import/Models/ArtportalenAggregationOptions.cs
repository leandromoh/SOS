﻿namespace SOS.Import.Models
{
    public class ArtportalenAggregationOptions
    {
        /// <summary>
        ///     The number of observations that will be fetched in each loop.
        /// </summary>
        public int ChunkSize { get; set; } = 1000000;

        /// <summary>
        ///     The number of sightings that should be harvested.
        ///     If set to null all sightings will be fetched.
        /// </summary>
        public int? MaxNumberOfSightingsHarvested { get; set; } = null;
    }
}