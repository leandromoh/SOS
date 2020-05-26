﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Shared;
using SOS.Process.Helpers.Interfaces;
using SOS.Process.Repositories.Destination.Interfaces;

namespace SOS.Process.Processors
{
    public abstract class ObservationProcessorBase<TEntity>
    {
        protected readonly IProcessedObservationRepository ProcessRepository;
        protected readonly ILogger<TEntity> Logger;
        protected readonly IFieldMappingResolverHelper FieldMappingResolverHelper;
        public abstract DataProviderType Type { get; }

        protected ObservationProcessorBase(
            IProcessedObservationRepository processedObservationRepository,
            IFieldMappingResolverHelper fieldMappingResolverHelper,
            ILogger<TEntity> logger)
        {
            ProcessRepository = processedObservationRepository ?? throw new ArgumentNullException(nameof(processedObservationRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            FieldMappingResolverHelper = fieldMappingResolverHelper ?? throw new ArgumentNullException(nameof(fieldMappingResolverHelper));
        }

        public virtual async Task<ProcessingStatus> ProcessAsync(
            DataProvider dataProvider,
            IDictionary<int, ProcessedTaxon> taxa,
            IJobCancellationToken cancellationToken)
        {
            Logger.LogDebug($"Start Processing {dataProvider} verbatim observations");
            var startTime = DateTime.Now;
            try
            {
                Logger.LogDebug($"Start deleting {dataProvider} data");
                if (!await ProcessRepository.DeleteProviderDataAsync(dataProvider))
                {
                    Logger.LogError($"Failed to delete {dataProvider} data");
                    return ProcessingStatus.Failed(dataProvider.Identifier, Type, startTime, DateTime.Now);
                }
                Logger.LogDebug($"Finish deleting {dataProvider} data");

                Logger.LogDebug($"Start processing {dataProvider} data");
                var verbatimCount = await ProcessObservations(dataProvider, taxa, cancellationToken);
                Logger.LogDebug($"Finish processing {dataProvider} data.");

                return ProcessingStatus.Success(dataProvider.Identifier, Type, startTime, DateTime.Now, verbatimCount);
            }
            catch (JobAbortedException)
            {
                Logger.LogInformation($"{dataProvider} observation processing was canceled.");
                return ProcessingStatus.Cancelled(dataProvider.Identifier, Type, startTime, DateTime.Now);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to process {dataProvider} sightings");
                return ProcessingStatus.Failed(dataProvider.Identifier, Type, startTime, DateTime.Now);
            }
        }

        protected abstract Task<int> ProcessObservations(
            DataProvider dataProvider,
            IDictionary<int, ProcessedTaxon> taxa,
            IJobCancellationToken cancellationToken);
        

        protected async Task<int> CommitBatchAsync(
            DataProvider dataProvider,
            ICollection<ProcessedObservation> processedObservations)
        {
            FieldMappingResolverHelper.ResolveFieldMappedValues(processedObservations);
            foreach (var observation in processedObservations)
            {
                observation.DataProviderId = dataProvider.Id;
            }

            var successCount = await ProcessRepository.AddManyAsync(processedObservations);

            return successCount;
        }

        protected bool IsBatchFilledToLimit(int count)
        {
            return count % ProcessRepository.BatchSize == 0;
        }
    }
}