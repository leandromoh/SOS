﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgileObjects.AgileMapper.Extensions;
using Elasticsearch.Net;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Enums;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.Cache.Interfaces;
using SOS.Lib.Factories;
using SOS.Lib.IO.DwcArchive.Interfaces;
using SOS.Lib.Jobs.Export;
using SOS.Lib.Jobs.Import;
using SOS.Lib.Jobs.Process;
using SOS.Lib.Managers.Interfaces;
using SOS.Lib.Models.Processed;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Processed.ProcessInfo;
using SOS.Lib.Models.Shared;
using SOS.Lib.Repositories.Processed.Interfaces;
using SOS.Lib.Repositories.Verbatim.Interfaces;
using SOS.Process.Managers.Interfaces;
using SOS.Process.Processors.Artportalen.Interfaces;
using SOS.Process.Processors.ClamPortal.Interfaces;
using SOS.Process.Processors.FishData.Interfaces;
using SOS.Process.Processors.Interfaces;
using SOS.Process.Processors.Kul.Interfaces;
using SOS.Process.Processors.Mvm.Interfaces;
using SOS.Process.Processors.Nors.Interfaces;
using SOS.Process.Processors.ObservationDatabase.Interfaces;
using SOS.Process.Processors.Sers.Interfaces;
using SOS.Process.Processors.Shark.Interfaces;
using SOS.Process.Processors.VirtualHerbarium.Interfaces;

namespace SOS.Process.Jobs
{
    /// <summary>
    ///     Artportalen harvest
    /// </summary>
    public class ProcessJob : ProcessJobBase, IProcessJob
    {
        private readonly IDwcArchiveFileWriterCoordinator _dwcArchiveFileWriterCoordinator;
        private readonly IAreaHelper _areaHelper;
        private readonly IDataProviderCache _dataProviderCache;
        private readonly IInstanceManager _instanceManager;
        private readonly IValidationManager _validationManager;
        private readonly ILogger<ProcessJob> _logger;
        private readonly IProcessedObservationRepository _processedObservationRepository;
        private readonly ICache<int, Taxon> _taxonCache;
        private readonly Dictionary<DataProviderType, IProcessor> _processorByType;
        private readonly IProcessTaxaJob _processTaxaJob;
        private readonly string _exportContainer;
        private readonly bool _runIncrementalAfterFull;
        private readonly long _minObservationCount;

        private async Task<IDictionary<int, Taxon>> GetTaxaAsync(JobRunModes mode)
        {
            // Use current taxa if we are in incremental mode, to speed things up
            if (mode == JobRunModes.Full)
            {
                //----------------------------------------------------------------------
                // Process taxa
                //----------------------------------------------------------------------
                _logger.LogInformation("Start processing taxonomy");

                if (!await _processTaxaJob.RunAsync())
                {
                    _logger.LogError("Failed to process taxonomy");
                    return null;
                }
                _taxonCache.Clear();
                _logger.LogInformation("Finish processing taxonomy");
            }

            //--------------------------------------
            // Get taxonomy
            //--------------------------------------
            _logger.LogInformation("Start getting processed taxa");

            var taxa = await _taxonCache.GetAllAsync();
            if (!taxa?.Any() ?? true)
            {
                _logger.LogWarning("Failed to get processed taxa");
                return null;
            }

            var taxaDictonary = new ConcurrentDictionary<int, Taxon>();
            taxa.ForEach(t => taxaDictonary.TryAdd(t.Id, t));
            
            _logger.LogInformation($"Finish getting processed taxa ({taxaDictonary.Count})");
            
            return taxaDictonary;
        }

        private async Task InitializeAreaHelperAsync()
        {
            _logger.LogDebug("Start initialize area cache");
            await _areaHelper.InitializeAsync();
            _logger.LogDebug("Finish initialize area cache");
        }

        private async Task InitializeElasticSearchAsync(JobRunModes mode)
        {
            if (mode == JobRunModes.Full)
            {
                _logger.LogInformation(
                    $"Start clear ElasticSearch index: {_processedObservationRepository.PublicIndexName}");
                await _processedObservationRepository.ClearCollectionAsync(false);
               
                _logger.LogInformation(
                    $"Finish clear ElasticSearch index: {_processedObservationRepository.PublicIndexName}");

                _logger.LogInformation(
                    $"Start clear ElasticSearch index: {_processedObservationRepository.ProtectedIndexName}");
                await _processedObservationRepository.ClearCollectionAsync(true);

                _logger.LogInformation(
                    $"Finish clear ElasticSearch index: {_processedObservationRepository.ProtectedIndexName}");
            }
            else
            {
                _logger.LogInformation($"Start ensure collection exists ({_processedObservationRepository.PublicIndexName})");
                // Create ES index ProcessedObservation-{0/1} if it doesn't exist.
                await _processedObservationRepository.VerifyCollectionAsync(false);
                _logger.LogInformation($"Finish ensure collection exists ({_processedObservationRepository.PublicIndexName})");

                _logger.LogInformation($"Start ensure collection exists ({_processedObservationRepository.ProtectedIndexName})");
                // Create ES index ProcessedObservation-{0/1} if it doesn't exist.
                await _processedObservationRepository.VerifyCollectionAsync(true);
                _logger.LogInformation($"Finish ensure collection exists ({_processedObservationRepository.ProtectedIndexName})");
            }
        }

        /// <summary>
        /// Disable Elasticsearch indexing
        /// </summary>
        /// <returns></returns>
        private async Task DisableIndexingAsync()
        {
            _logger.LogInformation($"Start disable indexing ({_processedObservationRepository.PublicIndexName})");
            await _processedObservationRepository.DisableIndexingAsync(false);
            _logger.LogInformation($"Finish disable indexing ({_processedObservationRepository.PublicIndexName})");

            _logger.LogInformation($"Start disable indexing ({_processedObservationRepository.ProtectedIndexName})");
            await _processedObservationRepository.DisableIndexingAsync(true);
            _logger.LogInformation($"Finish disable indexing ({_processedObservationRepository.ProtectedIndexName})");
        }

        /// <summary>
        /// Enable Elasticsearch indexing
        /// </summary>
        /// <returns></returns>
        private async Task EnableIndexingAsync()
        {
            _logger.LogInformation($"Start enable indexing ({_processedObservationRepository.PublicIndexName})");
            await _processedObservationRepository.EnableIndexingAsync(false);
            _logger.LogInformation($"Finish enable indexing ({_processedObservationRepository.PublicIndexName})");

            _logger.LogInformation($"Start enable indexing ({_processedObservationRepository.ProtectedIndexName})");
            await _processedObservationRepository.EnableIndexingAsync(true);
            _logger.LogInformation($"Finish enable indexing ({_processedObservationRepository.ProtectedIndexName})");
        }

        /// <summary>
        /// Validate that no protected data is accessable (undiffusedgf) from public index
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ValidateIndexesAsync()
        {
            var healthStatus = await _processedObservationRepository.GetHealthStatusAsync(WaitForStatus.Green);
            if (healthStatus == WaitForStatus.Red)
            {
                _logger.LogError("Elastich health status: Red");
                return false;
            }

            var publicCount = await _processedObservationRepository.IndexCountAsync(false);

            // Make sure we have a reasonable amount of observations processed
            if (publicCount < _minObservationCount)
            {
                _logger.LogError($"Validation failed. Only {publicCount} public observations processed. It should be at least {_minObservationCount}");
                return false;
            }

            var protectedCount = (int)await _processedObservationRepository.IndexCountAsync(true);
            if (protectedCount < 1)
            {
                _logger.LogError($"Validation failed. Only {protectedCount} protected observations processed");
                // No protected observations found. No more validation can be done
                return true;
            }

            var validationTasks = new[]
            {
                _processedObservationRepository.ValidateProtectionLevelAsync(false),
                _processedObservationRepository.ValidateProtectionLevelAsync(true),
                ValidateRandomObservations(),
                ValidateRandomObservations(),
                ValidateRandomObservations(),
                ValidateRandomObservations(),
                ValidateRandomObservations()
            };

            // Make sure no protected observations exists in public index and vice versa
            return (await Task.WhenAll(validationTasks)).All(t => t);
        }

        /// <summary>
        /// Validate 1000 random observations
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ValidateRandomObservations()
        {
            var observationsCount = 1000;

            // Get 1000 random observations from protected index
            var protectedObservations = (await _processedObservationRepository.GetRandomObservationsAsync(observationsCount, true))?
                .Where(o => o.Occurrence != null)
                .Distinct()
                .ToDictionary(o => o.Occurrence.OccurrenceId, o => o);

            if (protectedObservations?.Any() ?? false)
            {
                // Try to get diffused observations 
                var diffusedObservations = (await _processedObservationRepository.GetObservationsAsync(protectedObservations.Keys, false))?
                    .Where(o => o.Occurrence != null)
                    .ToDictionary(o => o.Occurrence.OccurrenceId, o => o); ;

                if (!diffusedObservations?.Any() ?? true)
                {
                    return true;
                }

                foreach (var protectedObservation in protectedObservations)
                {
                    // Try to get diffused observation with same occurenceId from public index
                    if (!diffusedObservations.TryGetValue(protectedObservation.Key, out var publicObservation))
                    {
                        continue;
                    }

                    // If observation coordinates equals, something is wrong. Validation failed
                    if (protectedObservation.Value.Location.DecimalLatitude.Equals(publicObservation.Location.DecimalLatitude)
                         || protectedObservation.Value.Location.DecimalLongitude.Equals(publicObservation.Location.DecimalLongitude)
                       )
                    {
                        var errorString = $"Failed to validate random observation coordinates. Coordinates match between protected and public index for observation with OccurrenceId: {protectedObservation.Value.Occurrence.OccurrenceId},";
                        errorString += $"Public coords:{publicObservation.Location.DecimalLatitude}, {publicObservation.Location.DecimalLongitude},";
                        errorString += $"Protected coords:{protectedObservation.Value.Location.DecimalLatitude}, {protectedObservation.Value.Location.DecimalLongitude},";
                        _logger.LogError(errorString);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///  Run process job
        /// </summary>
        /// <param name="dataProvidersToProcess"></param>
        /// <param name="mode"></param>
        /// <param name="copyFromActiveOnFail"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> RunAsync(
            IEnumerable<DataProvider> dataProvidersToProcess,
            JobRunModes mode,
            bool copyFromActiveOnFail,
            IJobCancellationToken cancellationToken)
        {
            try
            {
                //-----------------
                // 1. Arrange
                //-----------------
                _processedObservationRepository.LiveMode = mode == JobRunModes.IncrementalActiveInstance;

                //-----------------
                // 2. Validation
                //-----------------
                if (!dataProvidersToProcess.Any())
                {
                    return false;
                }

                //----------------------------------------------------------------------
                // 3. Initialization of meta data etc
                //----------------------------------------------------------------------
                var getTaxaTask = GetTaxaAsync(mode);
                await Task.WhenAll(getTaxaTask, InitializeAreaHelperAsync(), _validationManager.VerifyCollectionAsync(mode));

                var taxonById = await getTaxaTask;

                if ((taxonById?.Count ?? 0) == 0)
                {
                    return false;
                }

                cancellationToken?.ThrowIfCancellationRequested();

                // Init indexes
                await InitializeElasticSearchAsync(mode);

                if (mode == JobRunModes.Full)
                {
                    //------------------------------------------------------------------------
                    // 4. Start DWC file writing
                    //------------------------------------------------------------------------
                    _dwcArchiveFileWriterCoordinator.BeginWriteDwcCsvFiles();
                }

                // Disable indexing for public and protected index
                await DisableIndexingAsync();

                //------------------------------------------------------------------------
                // 5. Create observation processing tasks, and wait for them to complete
                //------------------------------------------------------------------------
                var result = await ProcessVerbatim(dataProvidersToProcess, mode, taxonById, copyFromActiveOnFail, cancellationToken);
                var success = result.All(t => t.Value.Status == RunStatus.Success);
                //---------------------------------
                // 6. Create ElasticSearch index
                //---------------------------------
                if (success)
                {
                  
                    // Enable indexing for public and protected index
                    await EnableIndexingAsync();

                    // Update dynamic provider data
                    await UpdateProvidersMetadataAsync(dataProvidersToProcess);

                    _logger.LogInformation($"Start validate indexes");
                    Thread.Sleep(TimeSpan.FromMinutes(1)); // Try wait for Elasticsearch index.
                    if (!await ValidateIndexesAsync())
                    {
                        throw new Exception("Validation of processed indexes failed. Job stopped to prevent leak of protected data");
                    }
                    _logger.LogInformation($"Finish validate indexes");

                    // Toggle active instance if we are done
                    if (mode == JobRunModes.Full && !_runIncrementalAfterFull || mode == JobRunModes.IncrementalInactiveInstance)
                    {
                        _logger.LogInformation($"Toggle instance {_processedObservationRepository.ActiveInstance} => {_processedObservationRepository.InActiveInstance}");
                        await _processedObservationRepository.SetActiveInstanceAsync(_processedObservationRepository
                            .InActiveInstance);
                    }

                    if (mode == JobRunModes.Full)
                    {
                        if (_runIncrementalAfterFull)
                        {
                            // Enqueue incremental harvest/process job to Hangfire in order to get latest sightings

                            var jobId = BackgroundJob.Enqueue<IObservationsHarvestJob>(job => job.RunIncrementalInactiveAsync(cancellationToken));

                            _logger.LogInformation($"Incremental harvest/process job with Id={jobId} was enqueued");
                        }
                        
                        //----------------------------------------------------------------------------
                        // 7. End create DwC CSV files and merge the files into multiple DwC-A files.
                        //----------------------------------------------------------------------------
                        var dwcFiles = await _dwcArchiveFileWriterCoordinator.CreateDwcaFilesFromCreatedCsvFiles();

                        if (dwcFiles?.Any() ?? false)
                        {
                            foreach (var dwcFile in dwcFiles)
                            {
                                // Enqueue upload file to blob storage job
                                var uploadJobId = BackgroundJob.Enqueue<IUploadToStoreJob>(job => job.RunAsync(dwcFile, _exportContainer, true,
                                    cancellationToken));

                                _logger.LogInformation($"Upload file to blob storage job with Id={uploadJobId} was enqueued");
                            }
                        }
                    }
                }

                _logger.LogInformation($"Processing done: {success} {mode}");

                //-------------------------------
                // 8. Return processing result
                //-------------------------------
                return success ? true : throw new Exception(
                    $@"Failed to process observations. {result
                        .Where(r => r.Value.Status != RunStatus.Success)
                        .Select(r => $"Provider: {r.Key} - {r.Value.Status}")}");
            }
            catch (JobAbortedException)
            {
                _logger.LogInformation("Process job was cancelled.");
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Process sightings job failed.");
                throw new Exception("Process sightings job failed.");
            }
            finally
            {
                if (mode == JobRunModes.Full)
                {
                    _dwcArchiveFileWriterCoordinator.DeleteTemporaryCreatedCsvFiles();
                }
            }
        }

        /// <summary>
        /// Process verbatim observations
        /// </summary>
        /// <param name="dataProvidersToProcess"></param>
        /// <param name="mode"></param>
        /// <param name="taxonById"></param>
        /// <param name="copyFromActiveOnFail"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IDictionary<DataProvider, ProcessingStatus>> ProcessVerbatim(IEnumerable<DataProvider> dataProvidersToProcess, JobRunModes mode, IDictionary<int, Taxon> taxonById, bool copyFromActiveOnFail, IJobCancellationToken cancellationToken)
        {
            var processStart = DateTime.Now;

            var processTaskByDataProvider = new Dictionary<DataProvider, Task<ProcessingStatus>>();
            foreach (var dataProvider in dataProvidersToProcess)
            {
                if (!dataProvider.IsActive || 
                    (mode != JobRunModes.Full && !dataProvider.SupportIncrementalHarvest))
                {
                    continue;
                }

                var processor = _processorByType[dataProvider.Type];
                processTaskByDataProvider.Add(dataProvider,
                    processor.ProcessAsync(dataProvider, taxonById, mode, cancellationToken));
            }

            var success = (await Task.WhenAll(processTaskByDataProvider.Values)).All(t => t.Status == RunStatus.Success);
            
            if (mode == JobRunModes.Full)
            {
                //----------------------------------------------------------------------------
                //  If a data provider failed to process and it was not Artportalen,
                //     then try to copy that data from the active instance.
                //----------------------------------------------------------------------------

                var artportalenSuccededOrDidntRun = !processTaskByDataProvider.Any(pair =>
                    pair.Key.Type == DataProviderType.ArtportalenObservations &&
                    pair.Value.Result.Status == RunStatus.Failed);

                if (!success && copyFromActiveOnFail && artportalenSuccededOrDidntRun)
                {
                    var copyTasks = processTaskByDataProvider
                        .Where(t => t.Value.Result.Status == RunStatus.Failed)
                        .Select(t => _instanceManager.CopyProviderDataAsync(t.Key)).ToArray();

                    await Task.WhenAll(copyTasks);
                    success = copyTasks.All(t => t.Result);
                }
            }

            await UpdateProcessInfoAsync(mode, processStart, processTaskByDataProvider, success);
            return processTaskByDataProvider.ToDictionary(pt => pt.Key, pt => pt.Value.Result);
        }

        /// <summary>
        /// Update process info
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="processStart"></param>
        /// <param name="processTaskByDataProvider"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        private async Task UpdateProcessInfoAsync(JobRunModes mode, 
            DateTime processStart, 
            IDictionary<DataProvider, 
                Task<ProcessingStatus>> processTaskByDataProvider,
            bool success)
        {

            if (!processTaskByDataProvider?.Any() ?? true)
            {
                return;
            }

            // Try to get process info for current instance
            var processInfo = await GetObservationProcessInfoAsync(mode == JobRunModes.IncrementalActiveInstance);

            if (processInfo == null || mode == JobRunModes.Full)
            {
                var providersInfo = new List<ProviderInfo>();

                foreach (var taskProvider in processTaskByDataProvider)
                {
                    var provider = taskProvider.Key;
                    var processResult = taskProvider.Value.Result;

                    if (processResult == null)
                    {
                        continue;
                    }

                    // Get harvest info and create a provider info object 
                    var harvestInfo = await GetHarvestInfoAsync(provider.Identifier);
                    var providerInfo = new ProviderInfo(provider)
                    {
                        HarvestCount = harvestInfo?.Count,
                        HarvestEnd = harvestInfo?.End,
                        HarvestNotes = harvestInfo?.Notes,
                        HarvestStart = harvestInfo?.Start,
                        HarvestStatus = harvestInfo?.Status,
                        PublicProcessCount = processResult.PublicCount,
                        ProtectedProcessCount = processResult.ProtectedCount,
                        ProcessEnd = processResult.End,
                        ProcessStart = processResult.Start,
                        ProcessStatus = processResult.Status
                    };

                    providersInfo.Add(providerInfo);
                }

                var metaDataProcessInfo = await GetProcessInfoAsync(new[]
                {
                        nameof(Lib.Models.Processed.Observation.Area),
                        nameof(Taxon)
                    });

                processInfo = new ProcessInfo(_processedObservationRepository.PublicIndexName, processStart)
                {
                    PublicCount = processTaskByDataProvider.Sum(pi => pi.Value.Result.PublicCount),
                    ProtectedCount = processTaskByDataProvider.Sum(pi => pi.Value.Result.ProtectedCount),
                    End = DateTime.Now,
                    MetadataInfo = metaDataProcessInfo,
                    ProvidersInfo = providersInfo,
                    Status = success ? RunStatus.Success : RunStatus.Failed
                };
            }
            else
            {
                foreach (var taskProvider in processTaskByDataProvider)
                {
                    var provider = taskProvider.Key;
                    var processResult = taskProvider.Value.Result;

                    if (processResult == null)
                    {
                        continue;
                    }

                    // Get provider info and update incremental values
                    var providerInfo = processInfo.ProvidersInfo.FirstOrDefault(pi => pi.DataProviderId == provider.Id);
                    if (providerInfo == null)
                    {
                        continue;
                    }

                    providerInfo.LatestIncrementalPublicCount = processResult.PublicCount;
                    providerInfo.LatestIncrementalProtectedCount = processResult.ProtectedCount;
                    providerInfo.LatestIncrementalEnd = processResult.End;
                    providerInfo.LatestIncrementalStart = processResult.Start;
                    providerInfo.LatestIncrementalStatus = processResult.Status;
                }
            }

            _logger.LogInformation("Start updating process info for observations");
            await SaveProcessInfo(processInfo);
            _logger.LogInformation("Finish updating process info for observations");
        }

        private async Task UpdateProvidersMetadataAsync(IEnumerable<DataProvider> providers)
        {
            foreach (var provider in providers.Where(p => p.SupportDynamicEml))
            {
                var eml = await _dataProviderCache.GetEmlAsync(provider.Id);

                if (eml == null)
                {
                    _logger.LogWarning($"No eml file found for provider: {provider.Identifier}");
                    continue;
                }

                // Get public meta data
                var metadata = await _processedObservationRepository.GetProviderMetaDataAsync(provider.Id, false);

                // Get protected meta data
                var protctedMetadata = await _processedObservationRepository.GetProviderMetaDataAsync(provider.Id, true);

                // Copmare public and protected and store peek values
                if ((protctedMetadata.firstSpotted ?? metadata.firstSpotted) < metadata.firstSpotted)
                {
                    metadata.firstSpotted = protctedMetadata.firstSpotted;
                }

                if ((protctedMetadata.lastSpotted ?? metadata.lastSpotted) < metadata.lastSpotted)
                {
                    metadata.lastSpotted = protctedMetadata.lastSpotted;
                }

                if (protctedMetadata.geographicCoverage.BottomRight.Lon > metadata.geographicCoverage.BottomRight.Lon)
                {
                    metadata.geographicCoverage.BottomRight.Lon = protctedMetadata.geographicCoverage.BottomRight.Lon;
                }

                if (protctedMetadata.geographicCoverage.BottomRight.Lat < metadata.geographicCoverage.BottomRight.Lat)
                {
                    metadata.geographicCoverage.BottomRight.Lat = protctedMetadata.geographicCoverage.BottomRight.Lat;
                }

                if (protctedMetadata.geographicCoverage.TopLeft.Lon < metadata.geographicCoverage.TopLeft.Lon)
                {
                    metadata.geographicCoverage.TopLeft.Lon = protctedMetadata.geographicCoverage.TopLeft.Lon;
                }

                if (protctedMetadata.geographicCoverage.TopLeft.Lat > metadata.geographicCoverage.TopLeft.Lat)
                {
                    metadata.geographicCoverage.BottomRight.Lat = protctedMetadata.geographicCoverage.BottomRight.Lat;
                }
                
                DwCArchiveEmlFileFactory.UpdateDynamicMetaData(eml, metadata.firstSpotted, metadata.lastSpotted, metadata.geographicCoverage);
                await _dataProviderCache.StoreEmlAsync(provider.Id, eml);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processedObservationRepository"></param>
        /// <param name="processInfoRepository"></param>
        /// <param name="harvestInfoRepository"></param>
        /// <param name="artportalenObservationProcessor"></param>
        /// <param name="clamPortalObservationProcessor"></param>
        /// <param name="fishDataObservationProcessor"></param>
        /// <param name="kulObservationProcessor"></param>
        /// <param name="mvmObservationProcessor"></param>
        /// <param name="norsObservationProcessor"></param>
        /// <param name="observationDatabaseProcessor"></param>
        /// <param name="sersObservationProcessor"></param>
        /// <param name="sharkObservationProcessor"></param>
        /// <param name="virtualHerbariumObservationProcessor"></param>
        /// <param name="dwcaObservationProcessor"></param>
        /// <param name="taxonCache"></param>
        /// <param name="dataProviderCache"></param>
        /// <param name="instanceManager"></param>
        /// <param name="validationManager"></param>
        /// <param name="processTaxaJob"></param>
        /// <param name="areaHelper"></param>
        /// <param name="dwcArchiveFileWriterCoordinator"></param>
        /// <param name="processConfiguration"></param>
        /// <param name="logger"></param>
        public ProcessJob(IProcessedObservationRepository processedObservationRepository,
            IProcessInfoRepository processInfoRepository,
            IHarvestInfoRepository harvestInfoRepository,
            IArtportalenObservationProcessor artportalenObservationProcessor,
            IClamPortalObservationProcessor clamPortalObservationProcessor,
            IFishDataObservationProcessor fishDataObservationProcessor,
            IKulObservationProcessor kulObservationProcessor,
            IMvmObservationProcessor mvmObservationProcessor,
            INorsObservationProcessor norsObservationProcessor,
            IObservationDatabaseProcessor observationDatabaseProcessor,
            ISersObservationProcessor sersObservationProcessor,
            ISharkObservationProcessor sharkObservationProcessor,
            IVirtualHerbariumObservationProcessor virtualHerbariumObservationProcessor,
            IDwcaObservationProcessor dwcaObservationProcessor,
            ICache<int, Taxon> taxonCache,
            IDataProviderCache dataProviderCache,
            IInstanceManager instanceManager,
            IValidationManager validationManager,
            IProcessTaxaJob processTaxaJob,
            IAreaHelper areaHelper,
            IDwcArchiveFileWriterCoordinator dwcArchiveFileWriterCoordinator,
            ProcessConfiguration processConfiguration,
            ILogger<ProcessJob> logger) : base(harvestInfoRepository, processInfoRepository)
        {
            _processedObservationRepository = processedObservationRepository ??
                                              throw new ArgumentNullException(nameof(processedObservationRepository));
            _dataProviderCache = dataProviderCache ?? throw new ArgumentNullException(nameof(dataProviderCache));
            _taxonCache = taxonCache ??
                          throw new ArgumentNullException(nameof(taxonCache));
            _processTaxaJob = processTaxaJob ?? throw new ArgumentNullException(nameof(processTaxaJob));
            _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
            _validationManager = validationManager ?? throw new ArgumentNullException(nameof(validationManager));
            _areaHelper = areaHelper ?? throw new ArgumentNullException(nameof(areaHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dwcArchiveFileWriterCoordinator = dwcArchiveFileWriterCoordinator ?? throw new ArgumentNullException(nameof(dwcArchiveFileWriterCoordinator));

            if (clamPortalObservationProcessor == null)
                throw new ArgumentNullException(nameof(clamPortalObservationProcessor));
            if (fishDataObservationProcessor == null) throw new ArgumentNullException(nameof(fishDataObservationProcessor));
            if (kulObservationProcessor == null) throw new ArgumentNullException(nameof(kulObservationProcessor));
            if (mvmObservationProcessor == null) throw new ArgumentNullException(nameof(mvmObservationProcessor));
            if (norsObservationProcessor == null) throw new ArgumentNullException(nameof(norsObservationProcessor));
            if (sersObservationProcessor == null) throw new ArgumentNullException(nameof(sersObservationProcessor));
            if (sharkObservationProcessor == null) throw new ArgumentNullException(nameof(sharkObservationProcessor));
            if (virtualHerbariumObservationProcessor == null)
                throw new ArgumentNullException(nameof(virtualHerbariumObservationProcessor));
            if (dwcaObservationProcessor == null) throw new ArgumentNullException(nameof(dwcaObservationProcessor));
            if (artportalenObservationProcessor == null)
                throw new ArgumentNullException(nameof(artportalenObservationProcessor));
            if (sharkObservationProcessor == null) throw new ArgumentNullException(nameof(sharkObservationProcessor));
            _processorByType = new Dictionary<DataProviderType, IProcessor>
            {
                {DataProviderType.ArtportalenObservations, artportalenObservationProcessor},
                {DataProviderType.ClamPortalObservations, clamPortalObservationProcessor},
                {DataProviderType.DwcA, dwcaObservationProcessor},
                {DataProviderType.BiologgObservations, dwcaObservationProcessor},
                {DataProviderType.FishDataObservations, fishDataObservationProcessor},
                {DataProviderType.KULObservations, kulObservationProcessor},
                {DataProviderType.MvmObservations, mvmObservationProcessor},
                {DataProviderType.NorsObservations, norsObservationProcessor},
                {DataProviderType.ObservationDatabase, observationDatabaseProcessor},
                {DataProviderType.SersObservations, sersObservationProcessor},
                {DataProviderType.SharkObservations, sharkObservationProcessor},
                {DataProviderType.VirtualHerbariumObservations, virtualHerbariumObservationProcessor},
                {DataProviderType.iNaturalistObservations, dwcaObservationProcessor}
            };

            _exportContainer = processConfiguration?.Export_Container ??
                               throw new ArgumentNullException(nameof(processConfiguration));
            _runIncrementalAfterFull = processConfiguration.RunIncrementalAfterFull;
            _minObservationCount = processConfiguration.MinObservationCount;
        }

        /// <inheritdoc />
        [DisplayName("Process Observations [Mode={1}]")]
        public async Task<bool> RunAsync(
            List<string> dataProviderIdOrIdentifiers,
            JobRunModes mode,
            IJobCancellationToken cancellationToken)
        {
            if (mode == JobRunModes.Full)
            {
                _dataProviderCache.Clear();
            }

            var allDataProviders = await _dataProviderCache.GetAllAsync();
            List<DataProvider> dataProvidersToProcess;
            if (dataProviderIdOrIdentifiers?.Any() ?? false)
            {
                dataProvidersToProcess = allDataProviders.Where(dataProvider =>
                        dataProviderIdOrIdentifiers.Any(dataProvider.EqualsIdOrIdentifier) && 
                        dataProvider.IsActive && 
                        (mode == JobRunModes.Full || dataProvider.SupportIncrementalHarvest))
                    .ToList();
            }
            else
            {
                dataProvidersToProcess = allDataProviders.Where(dataProvider =>
                        dataProvider.IsActive &&
                        (mode == JobRunModes.Full || dataProvider.SupportIncrementalHarvest))
                    .ToList();
            }

            return await RunAsync(
                dataProvidersToProcess,
                mode,
                false,
                cancellationToken);
        }

        /// <inheritdoc />
        [DisplayName("Process verbatim observations for all active providers")]
        public async Task<bool> RunAsync(
            bool copyFromActiveOnFail,
            IJobCancellationToken cancellationToken)
        {
            _dataProviderCache.Clear();

            var dataProviders = await _dataProviderCache.GetAllAsync();
            var dataProvidersToProcess = dataProviders.Where(dataProvider => dataProvider.IsActive).ToList();
            return await RunAsync(
                dataProvidersToProcess,
                JobRunModes.Full,
                copyFromActiveOnFail,
                cancellationToken);
        }
    }
}