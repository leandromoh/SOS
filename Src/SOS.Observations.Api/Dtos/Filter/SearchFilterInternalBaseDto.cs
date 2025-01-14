﻿namespace SOS.Observations.Api.Dtos.Filter
{
    /// <summary>
    /// Internal search filter.
    /// </summary>
    public class SearchFilterInternalBaseDto : SearchFilterBaseDto
    {
        /// <summary>
        /// Artportalen specific search properties
        /// </summary>
        public ExtendedFilterDto ExtendedFilter{ get; set; }
    }
}