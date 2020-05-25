﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Shared;

namespace SOS.Process.Repositories.Destination.Interfaces
{
    /// <summary>
    /// Processed data class
    /// </summary>
    public interface IProcessedObservationRepository : IProcessBaseRepository<ProcessedObservation, string>
    {
        public string IndexName { get; }

        Task<bool> ClearCollectionAsync();

        /// <summary>
        /// Add many items 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        new Task<int> AddManyAsync(IEnumerable<ProcessedObservation> items);

        /// <summary>
        /// Copy provider data from active instance to inactive instance.
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <returns></returns>
        Task<bool> CopyProviderDataAsync(DataProvider dataProvider);

        /// <summary>
        /// Delete provider data.
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <returns></returns>
        Task<bool> DeleteProviderDataAsync(DataProvider dataProvider);

        /// <summary>
        /// Create search index
        /// </summary>
        /// <returns></returns>
        Task CreateIndexAsync();
    }
}
