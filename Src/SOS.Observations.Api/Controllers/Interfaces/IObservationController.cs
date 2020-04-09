﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SOS.Lib.Models.Search;

namespace SOS.Observations.Api.Controllers.Interfaces
{
    /// <summary>
    /// Sighting controller interface
    /// </summary>
    public interface IObservationController
    {
        /// <summary>
        /// Search for observations by the provided filter. All permitted values are either specified in the Field Mappings object
        /// retrievable from the Field Mappings endpoint or by the range of the underlying data type. All fields containing
        /// the substring "Id" (but not exclusively) are mapped in the Field Mappings object.
        /// </summary>
        /// <param name="filter">Filter used to limit the search</param>
        /// <param name="skip">Start index of returned observations</param>
        /// <param name="take">End index of returned observations</param>
        /// <returns>List of matching observations</returns>
        Task<IActionResult> GetChunkAsync(SearchFilter filter, int skip, int take);

        /// <summary>
        /// Field Mappings are used for properties with multiple acceptable fixed values but limited by other contraints then permitted by
        /// the underlying data type. E.g gender can have the values: male, female...
        /// 
        /// Field Mappings also describe the different possible query parameters available in searches.
        /// </summary>
        /// <returns>List of Field Mappings</returns>
        Task<IActionResult> GetFieldMappingAsync();
    }
}
