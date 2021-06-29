﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;
using SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;

namespace SOS.Lib.Repositories.Processed.Interfaces
{
    /// <summary>
    ///     Processed data class
    /// </summary>
    public interface IProcessedObservationRepositoryBase : IProcessRepositoryBase<Observation>
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
        /// Delete observations by occurence id
        /// </summary>
        /// <param name="occurenceIds"></param>
        Task<bool> DeleteByOccurrenceIdAsync(IEnumerable<string> occurenceIds);

        /// <summary>
        /// Turn of indexing
        /// </summary>
        /// <returns></returns>
        Task<bool> DisableIndexingAsync();

        /// <summary>
        ///     Turn on indexing
        /// </summary>
        /// <returns></returns>
        Task EnableIndexingAsync();

        /// <summary>
        /// Get index health status
        /// </summary>
        /// <param name="waitForStatus"></param>
        /// <returns></returns>
        Task<WaitForStatus> GetHealthStatusAsync(WaitForStatus waitForStatus);

        /// <summary>
        /// Get index name
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        string GetIndexName(byte instance);

        /// <summary>
        /// Get latest modified document date for passed provider
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        Task<DateTime> GetLatestModifiedDateForProviderAsync(int providerId);

        /// <summary>
        /// Get provider meta data
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        Task<(DateTime? firstSpotted, DateTime? lastSpotted, GeoBounds geographicCoverage)> GetProviderMetaDataAsync(
            int providerId);

        /// <summary>
        /// Count documents in index
        /// </summary>
        /// <returns></returns>
        Task<long> IndexCountAsync();

        /// <summary>
        ///     Get observation by scroll. 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <returns></returns>
        Task<ScrollResult<Observation>> ScrollObservationsAsync(
            FilterBase filter,
            string scrollId);

        /// <summary>
        /// Get observations by their occurrence id's
        /// </summary>
        /// <param name="occurrenceIds"></param>
        /// <param name="outputFields"></param>
        /// <returns></returns>
        Task<IEnumerable<Observation>> GetObservationsByOccurrenceIdsAsync(
            IEnumerable<string> occurrenceIds,
            IEnumerable<string> outputFields);

        /// <summary>
        ///     Get multimedia.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <returns></returns>
        Task<ScrollResult<SimpleMultimediaRow>> ScrollMultimediaAsync(
            FilterBase filter,
            string scrollId);

        /// <summary>
        /// Get measurementOrFacts.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="scrollId"></param>
        /// <returns></returns>
        Task<ScrollResult<ExtendedMeasurementOrFactRow>> ScrollMeasurementOrFactsAsync(
            FilterBase filter,
            string scrollId);

        /// <summary>
        /// Make sure no protected observations are in public index or no public observations are in protected index
        /// </summary>
        /// <returns>true if everything is fine</returns>
        Task<bool> ValidateProtectionLevelAsync();

        /// <summary>
        /// Verify that collection exists
        /// </summary>
        /// <returns></returns>
        Task<bool> VerifyCollectionAsync();

        /// <summary>
        /// Batch size used for write
        /// </summary>
        int WriteBatchSize { get; set; }
    }
}