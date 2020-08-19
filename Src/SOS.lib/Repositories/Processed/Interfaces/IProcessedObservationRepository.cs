﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;

namespace SOS.Lib.Repositories.Processed.Interfaces
{
    /// <summary>
    ///     Processed data class
    /// </summary>
    public interface IProcessedObservationRepository : IProcessRepositoryBase<ProcessedObservation>
    {
        public string IndexName { get; }

        Task<bool> ClearCollectionAsync();

        /// <summary>
        ///     Add many items
        /// </summary>
        /// <param name="observations"></param>
        /// <returns></returns>
        new Task<int> AddManyAsync(IEnumerable<ProcessedObservation> observations);

        /// <summary>
        ///     Copy provider data from active instance to inactive instance.
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <returns></returns>
        Task<bool> CopyProviderDataAsync(DataProvider dataProvider);

        /// <summary>
        ///     Delete provider data.
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <returns></returns>
        Task<bool> DeleteProviderDataAsync(DataProvider dataProvider);

        /// <summary>
        /// Delete observations by occurence id
        /// </summary>
        /// <param name="occurenceIds"></param>
        Task<bool> DeleteByOccurenceIdAsync(IEnumerable<string> occurenceIds);

        /// <summary>
        ///     Create search index
        /// </summary>
        /// <returns></returns>
        Task CreateIndexAsync();

        /// <summary>
        /// Get max id for specified provider
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        Task<int> GetMaxIdForProviderAsync(int providerId);

        /// <summary>
        /// Verify that collection exists
        /// </summary>
        /// <returns></returns>
        Task<bool> VerifyCollectionAsync();
    }
}