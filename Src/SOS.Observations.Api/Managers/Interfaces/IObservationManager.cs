﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Lib.Models.Search;

namespace SOS.Observations.Api.Managers.Interfaces
{
    /// <summary>
    /// Observation manager interface
    /// </summary>
    public interface IObservationManager
    {
        /// <summary>
        /// Get chunk of sightings
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        Task<IEnumerable<dynamic>> GetChunkAsync(SearchFilter filter, int skip, int take);
    }
}