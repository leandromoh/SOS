﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SOS.Export.IO.DwcArchive.Interfaces;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Constants;
using SOS.Lib.Enums;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Interfaces;
using SOS.Lib.Models.Processed;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Lib.Repositories.Verbatim.Interfaces;
using SOS.Process.Managers.Interfaces;
using SOS.Process.Processors.Interfaces;

namespace SOS.Process.Processors
{
    public abstract class ObservationProcessorBase<TClass, TVerbatim, TFactory, TVerbatimRepository> 
        where TVerbatim : IEntity<int> 
        where TFactory : IObservationFactory<TVerbatim> 
        where TVerbatimRepository : IVerbatimRepositoryBase<TVerbatim, int>
    {
        private readonly IProcessManager _processManager;
        private readonly IDiffusionManager _diffusionManager;
        private readonly bool _handleProtected;

        /// <summary>
        /// Commit batch
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="protectedData"></param>
        /// <param name="processedObservations"></param>
        /// <param name="batchId"></param>
        /// <param name="attempt"></param>
        /// <returns></returns>
        private async Task<int> CommitBatchAsync(
            DataProvider dataProvider,
            bool protectedData,
            ICollection<Observation> processedObservations,
            string batchId,
            byte attempt = 1)
        {
            try
            {
                if (vocabularyValueResolver.Configuration.ResolveValues)
                {
                    // used for testing purpose for easier debugging of vocabulary mapped data.
                    vocabularyValueResolver
                        .ResolveVocabularyMappedValues(
                            processedObservations);
                }

                Logger.LogDebug($"Start storing {dataProvider.Identifier} batch: {batchId}");
                var processedCount = protectedData
                    ? await ProtectedRepository.AddManyAsync(processedObservations)
                    : await PublicRepository.AddManyAsync(processedObservations);
                Logger.LogDebug($"Finish storing {dataProvider.Identifier} batch: {batchId} ({processedCount})");

                return processedCount;
            }
            catch (Exception e)
            {
                if (attempt < 3)
                {
                    Logger.LogWarning(e, $"Failed to commit batch: {batchId} for {dataProvider}, attempt: {attempt}");
                    System.Threading.Thread.Sleep(attempt * 200);
                    attempt++;
                    return await CommitBatchAsync(dataProvider, protectedData, processedObservations, batchId, attempt);
                }

                Logger.LogError(e, $"Failed to commit batch:{batchId} for {dataProvider}");
                throw;
            }

        }

        /// <summary>
        /// Delete batch
        /// </summary>
        /// <param name="occurrenceIds"></param>
        /// <returns></returns>
        private async Task<bool> DeleteBatchAsync(
            bool protectedObservations,
            ICollection<string> occurrenceIds)
        {
            try
            {
                return protectedObservations ?
                    await ProtectedRepository.DeleteByOccurrenceIdAsync(occurrenceIds) :
                    await PublicRepository.DeleteByOccurrenceIdAsync(occurrenceIds);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to delete batch by occurrence id's");
                return false;
            }
        }

        /// <summary>
        /// Process a batch of data
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="startId"></param>
        /// <param name="endId"></param>
        /// <param name="mode"></param>
        /// <param name="observationFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<(int publicCount, int protectedCount)> ProcessBatchAsync(
            DataProvider dataProvider,
            int startId,
            int endId,
            JobRunModes mode,
            TFactory observationFactory,
            TVerbatimRepository observationVerbatimRepository,
            IJobCancellationToken cancellationToken)
        {
            try
            {
                cancellationToken?.ThrowIfCancellationRequested();
                Logger.LogDebug($"Start fetching {dataProvider.Identifier} batch ({startId}-{endId})");
                var verbatimObservationsBatch = await observationVerbatimRepository.GetBatchAsync(startId, endId);
                Logger.LogDebug($"Finish fetching {dataProvider.Identifier} batch ({startId}-{endId})");

                if (!verbatimObservationsBatch?.Any() ?? true)
                {
                    return (0, 0);
                }

                Logger.LogDebug($"Start processing {dataProvider.Identifier} batch ({startId}-{endId})");

                var publicObservations = new List<Observation>();
                var protectedObservations = new List<Observation>();
               
                foreach (var verbatimObservation in verbatimObservationsBatch)
                {
                    cancellationToken?.ThrowIfCancellationRequested();

                    var observation = observationFactory.CreateProcessedObservation(verbatimObservation);

                    if (observation == null)
                    {
                        continue;
                    }

                    // If provider handle protected observations and observation is protected
                    if (_handleProtected && observation.Occurrence.ProtectionLevel > 2)
                    {
                        observation.Protected = true;
                        protectedObservations.Add(observation);

                        //If it is a protected sighting, public users should not be possible to find it in the current month 
                        if (!EnableDiffusion || ((observation.Event?.StartDate?.Year ?? 0) == DateTime.Now.Year || (observation?.Event?.EndDate?.Year ?? 0) == DateTime.Now.Year) &&
                            ((observation.Event?.StartDate?.Month ?? 0) == DateTime.Now.Month || (observation?.Event?.EndDate?.Month ?? 0) == DateTime.Now.Month))
                        {
                            continue;
                        }

                        // Recreate observation to make a new object
                        observation = observationFactory.CreateProcessedObservation(verbatimObservation);
                        // Diffuse protected observation before adding it to public index. Clone it to not affect protected obs
                        _diffusionManager.DiffuseObservation(observation);
                    }

                    // Add public observation
                    publicObservations.Add(observation);
                }

                Logger.LogDebug($"Finish processing {dataProvider.Identifier} batch ({startId}-{endId})");

                var validateAndStoreTasks = new List<Task<int>>
                {
                    ValidateAndStoreObservations(dataProvider, mode, false, publicObservations, $"{startId}-{endId}")
                };
                if (_handleProtected)
                {
                    validateAndStoreTasks.Add(ValidateAndStoreObservations(dataProvider, mode, true, protectedObservations, $"{startId}-{endId}"));
                }

                await Task.WhenAll(validateAndStoreTasks);

                var publicCount = validateAndStoreTasks[0].Result;
                var protectedCount = 0;
                if (_handleProtected)
                {
                    protectedCount = validateAndStoreTasks[1].Result;
                }

                return (publicCount, protectedCount);
            }
            catch (JobAbortedException e)
            {
                // Throw cancelation again to let function above handle it
                throw;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Process {dataProvider.Identifier} sightings from id: {startId} to id: {endId} failed");
            }
            finally
            {
                _processManager.Release();
            }

            return (0, 0);
        }

        /// <summary>
        /// Resolve vocabulary mapped values and then write the observations to DwC-A CSV files.
        /// </summary>
        /// <param name="processedObservations"></param>
        /// <param name="dataProvider"></param>
        /// <param name="batchId"></param>
        /// <returns></returns>
        private async Task<bool> WriteObservationsToDwcaCsvFiles(
            IEnumerable<Observation> processedObservations,
            DataProvider dataProvider,
            string batchId = "")
        {

            Logger.LogDebug($"Start writing {dataProvider.Identifier} CSV ({batchId})");
            vocabularyValueResolver.ResolveVocabularyMappedValues(processedObservations, Cultures.en_GB, true);
            var success = await dwcArchiveFileWriterCoordinator.WriteObservations(processedObservations, dataProvider, batchId);

            Logger.LogDebug($"Finish writing {dataProvider.Identifier} CSV ({batchId}) - {success}");

            return success;
        }

        protected readonly IDwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator;
        protected readonly IVocabularyValueResolver vocabularyValueResolver;
        protected readonly ILogger<TClass> Logger;
        protected readonly IProcessedPublicObservationRepository PublicRepository;
        protected readonly IProcessedProtectedObservationRepository ProtectedRepository;
        protected readonly IValidationManager ValidationManager;

        protected bool EnableDiffusion { get; }

        /// <summary>
        /// Constructor for public only 
        /// </summary>
        /// <param name="processedPublicObservationRepository"></param>
        /// <param name="vocabularyValueResolver"></param>
        /// <param name="dwcArchiveFileWriterCoordinator"></param>
        /// <param name="validationManager"></param>
        /// <param name="processManager"></param>
        /// <param name="logger"></param>
        protected ObservationProcessorBase(IProcessedPublicObservationRepository processedPublicObservationRepository,
            IVocabularyValueResolver vocabularyValueResolver,
            IDwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator,
            IValidationManager validationManager,
            IProcessManager processManager,
            ILogger<TClass> logger) : this(processedPublicObservationRepository, null, vocabularyValueResolver, dwcArchiveFileWriterCoordinator, validationManager,null, processManager, new ProcessConfiguration{Diffusion = false}, logger)
        {
        }

        /// <summary>
        /// Constructor for public and protected
        /// </summary>
        /// <param name="processedPublicObservationRepository"></param>
        /// <param name="processedProtectedObservationRepository"></param>
        /// <param name="vocabularyValueResolver"></param>
        /// <param name="dwcArchiveFileWriterCoordinator"></param>
        /// <param name="validationManager"></param>
        /// <param name="diffusionManager"></param>
        /// <param name="processConfiguration"></param>
        /// <param name="logger"></param>
        protected ObservationProcessorBase(IProcessedPublicObservationRepository processedPublicObservationRepository,
            IProcessedProtectedObservationRepository processedProtectedObservationRepository,
            IVocabularyValueResolver vocabularyValueResolver,
            IDwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator,
            IValidationManager validationManager,
            IDiffusionManager diffusionManager,
            IProcessManager processManager,
            ProcessConfiguration processConfiguration,
            ILogger<TClass> logger)
        {
            _handleProtected = processedProtectedObservationRepository != null;

           PublicRepository = processedPublicObservationRepository ??
                               throw new ArgumentNullException(nameof(processedPublicObservationRepository));

            this.vocabularyValueResolver = vocabularyValueResolver ??
                                           throw new ArgumentNullException(nameof(vocabularyValueResolver));
            this.dwcArchiveFileWriterCoordinator = dwcArchiveFileWriterCoordinator ?? throw new ArgumentNullException(nameof(dwcArchiveFileWriterCoordinator));
            ValidationManager = validationManager ?? throw new ArgumentNullException(nameof(validationManager));

            if (_handleProtected)
            {
                ProtectedRepository = processedProtectedObservationRepository;
                _diffusionManager = diffusionManager ?? throw new ArgumentNullException(nameof(diffusionManager));
            }

            EnableDiffusion = processConfiguration?.Diffusion ?? false;
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

           
        }

        protected abstract Task<(int publicCount, int protectedCount)> ProcessObservations(
            DataProvider dataProvider,
            IDictionary<int, Lib.Models.Processed.Observation.Taxon> taxa,
            JobRunModes mode,
            IJobCancellationToken cancellationToken);

        /// <inheritdoc />
        protected async Task<(int publicCount, int protectedCount)> ProcessObservationsAsync(
            DataProvider dataProvider,
            JobRunModes mode,
            TFactory observationFactory,
            TVerbatimRepository observationVerbatimRepository,
            IJobCancellationToken cancellationToken)
        {
            var startId = 1;
            var maxId = await observationVerbatimRepository.GetMaxIdAsync();
            var processBatchTasks = new List<Task<(int publicCount, int protectedCount)>>();

            while (startId <= maxId)
            {
                await _processManager.WaitAsync();

                var batchEndId = startId + WriteBatchSize - 1;
                processBatchTasks.Add(ProcessBatchAsync(
                    dataProvider,
                    startId, 
                    batchEndId, 
                    mode, 
                    observationFactory,
                    observationVerbatimRepository,
                    cancellationToken));
                startId = batchEndId + 1;
            }

            await Task.WhenAll(processBatchTasks);

            return (processBatchTasks.Sum(t => t.Result.publicCount), processBatchTasks.Sum(t => t.Result.protectedCount));
        }

        protected async Task<int> ValidateAndStoreObservations(DataProvider dataProvider, JobRunModes mode, bool protectedObservations, ICollection<Observation> observations, string batchId)
        {
            if (!observations?.Any() ?? true)
            {
                return 0;
            }

            observations =
                await ValidateAndRemoveInvalidObservations(dataProvider, observations, batchId);

            if (!observations?.Any() ?? true)
            {
                return 0;
            }

            if (mode != JobRunModes.Full)
            {
                Logger.LogDebug($"Start deleting {dataProvider.Identifier} live data {batchId}");
                var occurrenceIds = observations.Select(o => o.Occurrence.OccurrenceId).ToArray();
                var success = await DeleteBatchAsync(protectedObservations, occurrenceIds);

                // If provider supports protected observations and diffusion is disabled,
                // make sure the observation don't exists in both public and protected index
                if (dataProvider.SupportProtectedHarvest && !EnableDiffusion)
                {
                    success = await DeleteBatchAsync(!protectedObservations, occurrenceIds);
                }

                Logger.LogDebug($"Finish deleting {dataProvider.Identifier} live data {batchId}: {success}");
            }

            var processedCount = await CommitBatchAsync(dataProvider, protectedObservations, observations, batchId);

            if (mode == JobRunModes.Full && !protectedObservations)
            {
                await WriteObservationsToDwcaCsvFiles(observations, dataProvider, batchId);
            }
            observations.Clear();

            return processedCount;
        }

        protected async Task<ICollection<Observation>> ValidateAndRemoveInvalidObservations(
            DataProvider dataProvider,
            ICollection<Observation> observations,
            string batchId)
        {
            Logger.LogDebug($"Start validating {dataProvider.Identifier} batch: {batchId}");
            var invalidObservations = ValidationManager.ValidateObservations(ref observations, dataProvider);
            await ValidationManager.AddInvalidObservationsToDb(invalidObservations);
            Logger.LogDebug($"End validating {dataProvider.Identifier} batch: {batchId}");

            return observations;
        }

        protected int WriteBatchSize => PublicRepository.WriteBatchSize;

        public abstract DataProviderType Type { get; }

        /// <summary>
        /// Process observations
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="taxa"></param>
        /// <param name="mode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<ProcessingStatus> ProcessAsync(
            DataProvider dataProvider,
            IDictionary<int, Lib.Models.Processed.Observation.Taxon> taxa,
            JobRunModes mode,
            IJobCancellationToken cancellationToken)
        {
            Logger.LogInformation($"Start Processing {dataProvider.Identifier} verbatim observations");
            var startTime = DateTime.Now;

            try
            {
                if (mode == JobRunModes.Full)
                {
                    Logger.LogDebug($"Start deleting {dataProvider.Identifier} data");
                    if (!await PublicRepository.DeleteProviderDataAsync(dataProvider))
                    {
                        Logger.LogError($"Failed to delete {dataProvider.Identifier} public data");
                    }

                    if (dataProvider.SupportProtectedHarvest && !await ProtectedRepository.DeleteProviderDataAsync(dataProvider))
                    {
                        Logger.LogError($"Failed to delete {dataProvider.Identifier} protected data");
                    }

                    Logger.LogDebug($"Finish deleting {dataProvider.Identifier} data");
                }

                Logger.LogDebug($"Start processing {dataProvider.Identifier} data");
                var processCount = await ProcessObservations(dataProvider, taxa, mode, cancellationToken);

                Logger.LogInformation($"Finish processing {dataProvider.Identifier} data.");

                return ProcessingStatus.Success(dataProvider.Identifier, Type, startTime, DateTime.Now, processCount.publicCount, processCount.protectedCount);
            }
            catch (JobAbortedException)
            {
                Logger.LogInformation($"{dataProvider.Identifier} observation processing was canceled.");
                return ProcessingStatus.Cancelled(dataProvider.Identifier, Type, startTime, DateTime.Now);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to process {dataProvider.Identifier} sightings");
                return ProcessingStatus.Failed(dataProvider.Identifier, Type, startTime, DateTime.Now);
            }
        }
    }
}