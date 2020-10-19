﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;

namespace SOS.Lib.Repositories.Processed.Interfaces
{
    /// <summary>
    ///     Processed data class
    /// </summary>
    public interface IProcessedObservationRepository : IProcessRepositoryBase<Observation>
    {
        public string IndexName { get; }

        Task<bool> ClearCollectionAsync();

        /// <summary>
        ///     Add many items
        /// </summary>
        /// <param name="observations"></param>
        /// <returns></returns>
        new Task<int> AddManyAsync(IEnumerable<Observation> observations);

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
        /// Delete provider batch
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="verbatimIds"></param>
        /// <returns></returns>
        Task<bool> DeleteProviderBatchAsync(DataProvider dataProvider, ICollection<int> verbatimIds);

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
        /// Get latest modified document date for passed provider
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        Task<DateTime> GetLatestModifiedDateForProviderAsync(int providerId);

        /// <summary>
        ///     Get project parameters.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <returns></returns>
        Task<ScrollResult<Project>> ScrollProjectParametersAsync(FilterBase filter, string scrollId);

        /// <summary>
        ///     Get observation by scroll
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <returns></returns>
        Task<ScrollResult<Observation>> ScrollObservationsAsync(FilterBase filter, string scrollId);

        /// <summary>
        ///     Get observation by scroll. 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <remarks>To improve performance this method doesn't use the dynamic type.</remarks>
        /// <returns></returns>
        Task<ScrollResult<Observation>> TypedScrollObservationsAsync(
            FilterBase filter,
            string scrollId);

        /// <summary>
        ///     Get project parameters.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <remarks>To improve performance this method doesn't use the dynamic type.</remarks>
        /// <returns></returns>
        Task<ScrollResult<ExtendedMeasurementOrFactRow>> TypedScrollProjectParametersAsync(
            FilterBase filter,
            string scrollId);

        /// <summary>
        /// Verify that collection exists
        /// </summary>
        /// <returns></returns>
        Task<bool> VerifyCollectionAsync();
    }
}