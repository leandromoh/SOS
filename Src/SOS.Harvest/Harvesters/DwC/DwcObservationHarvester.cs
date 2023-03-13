﻿using System.Xml.Linq;
using DwC_A;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using SOS.Harvest.DarwinCore;
using SOS.Harvest.DarwinCore.Interfaces;
using SOS.Harvest.Harvesters.DwC.Interfaces;
using SOS.Lib.Configuration.Import;
using SOS.Lib.Database.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.DarwinCore;
using SOS.Lib.Models.Verbatim.Shared;
using SOS.Lib.Repositories.Resource.Interfaces;
using SOS.Lib.Repositories.Verbatim;
using SOS.Lib.Services.Interfaces;

namespace SOS.Harvest.Harvesters.DwC
{
    /// <summary>
    ///     DwC-A observation harvester.
    /// </summary>
    public class DwcObservationHarvester : IDwcObservationHarvester
    {
        private readonly IVerbatimClient _verbatimClient;
        private readonly IDwcArchiveReader _dwcArchiveReader;
        private readonly IFileDownloadService _fileDownloadService;
        private readonly IDataProviderRepository _dataProviderRepository;
        private readonly DwcaConfiguration _dwcaConfiguration;
        private readonly ILogger<DwcObservationHarvester> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="verbatimClient"></param>
        /// <param name="dwcArchiveReader"></param>
        /// <param name="fileDownloadService"></param>
        /// <param name="dataProviderRepository"></param>
        /// <param name="dwcaConfiguration"></param>
        /// <param name="logger"></param>
        public DwcObservationHarvester(
            IVerbatimClient verbatimClient,
            IDwcArchiveReader dwcArchiveReader,
            IFileDownloadService fileDownloadService,
            IDataProviderRepository dataProviderRepository,
            DwcaConfiguration dwcaConfiguration,
            ILogger<DwcObservationHarvester> logger)
        {
            _verbatimClient = verbatimClient ?? throw new ArgumentNullException(nameof(verbatimClient));
            _dwcArchiveReader = dwcArchiveReader ?? throw new ArgumentNullException(nameof(dwcArchiveReader));
            _fileDownloadService = fileDownloadService ?? throw new ArgumentNullException(nameof(fileDownloadService));
            _dataProviderRepository =
                dataProviderRepository ?? throw new ArgumentNullException(nameof(dataProviderRepository));
            _dwcaConfiguration = dwcaConfiguration ?? throw new ArgumentNullException(nameof(dwcaConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get EML XML document.
        /// </summary>
        /// <param name="archivePath"></param>
        /// <returns></returns>
        public XDocument GetEmlXmlDocument(string archivePath)
        {
            using var archiveReader = new ArchiveReader(archivePath, _dwcaConfiguration.ImportPath);
            var emlFile = archiveReader.GetEmlXmlDocument();
            return emlFile;
        }

        /// <summary>
        ///     Harvest DwC Archive observations
        /// </summary>
        /// <returns></returns>
        public async Task<HarvestInfo> HarvestObservationsAsync(
            string archivePath,
            DataProvider dataProvider,
            IJobCancellationToken cancellationToken)
        {
            if (_dwcaConfiguration.UseDwcaCollectionRepository)
            {
                return await HarvestObservationsUsingCollectionDwcaAsync(archivePath, dataProvider, cancellationToken);
            }

            return await HarvestObservationsUsingDwcaAsync(archivePath, dataProvider, cancellationToken);
        }

        private async Task<HarvestInfo> HarvestObservationsUsingDwcaAsync(
            string archivePath,
            DataProvider dataProvider,
            IJobCancellationToken cancellationToken)
        {
            var harvestInfo = new HarvestInfo(dataProvider.Identifier, DateTime.Now);
            harvestInfo.Id = dataProvider.Identifier;
            using var dwcArchiveVerbatimRepository = new DarwinCoreArchiveVerbatimRepository(dataProvider, _verbatimClient, _logger) { TempMode = true };

            try
            {
                _logger.LogDebug($"Start clearing DwC-A observations for {dataProvider.Identifier}");
                await dwcArchiveVerbatimRepository.DeleteCollectionAsync();
                await dwcArchiveVerbatimRepository.AddCollectionAsync();
                _logger.LogDebug($"Finish clearing DwC-A observations for {dataProvider.Identifier}");

                _logger.LogDebug($"Start storing DwC-A observations for {dataProvider.Identifier}");
                var observationCount = 0;
                using var archiveReader = new ArchiveReader(archivePath, _dwcaConfiguration.ImportPath);

                var observationBatches =
                    _dwcArchiveReader.ReadArchiveInBatchesAsync(archiveReader, dataProvider,
                        _dwcaConfiguration.BatchSize);
                await foreach (var verbatimObservationsBatch in observationBatches)
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                    if (_dwcaConfiguration.MaxNumberOfSightingsHarvested.HasValue &&
                        observationCount >= _dwcaConfiguration.MaxNumberOfSightingsHarvested)
                    {
                        _logger.LogInformation($"Max observations for {dataProvider.Identifier} reached");
                        break;
                    }

                    observationCount += verbatimObservationsBatch.Count();
                    await dwcArchiveVerbatimRepository.AddManyAsync(verbatimObservationsBatch);
                }                

                if (dataProvider.UseVerbatimFileInExport)
                {
                    _logger.LogDebug($"Start storing source file for {dataProvider.Identifier}");
                    await using var fileStream = File.OpenRead(archivePath);
                    await dwcArchiveVerbatimRepository.StoreSourceFileAsync(dataProvider.Id, fileStream);
                    _logger.LogDebug($"Finish storing source file for {dataProvider.Identifier}");
                }

                _logger.LogDebug($"Finish storing DwC-A observations for {dataProvider.Identifier}");

                // Update harvest info
                harvestInfo.End = DateTime.Now;
                harvestInfo.Status = RunStatus.Success;
                harvestInfo.Count = observationCount;

                _logger.LogInformation($"Start permanentize temp collection for {dataProvider.Identifier}");
                await dwcArchiveVerbatimRepository.PermanentizeCollectionAsync();
                _logger.LogInformation($"Finish permanentize temp collection for {dataProvider.Identifier}");
            }
            catch (JobAbortedException e)
            {
                _logger.LogError(e, $"Canceled harvest of DwC Archive for {dataProvider.Identifier}");
                harvestInfo.Status = RunStatus.Canceled;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed harvest of DwC Archive for {dataProvider.Identifier}");
                harvestInfo.Status = RunStatus.Failed;
            }

            return harvestInfo;
        }

        private async Task<HarvestInfo> HarvestObservationsUsingCollectionDwcaAsync(
            string archivePath,
            DataProvider dataProvider,
            IJobCancellationToken cancellationToken)
        {
            var harvestInfo = new HarvestInfo(dataProvider.Identifier, DateTime.Now);
            harvestInfo.Id = dataProvider.Identifier;
            using var dwcArchiveVerbatimRepository = new DarwinCoreArchiveVerbatimRepository(dataProvider, _verbatimClient, _logger) { TempMode = true };

            try
            {                
                using var archiveReader = new ArchiveReader(archivePath, _dwcaConfiguration.ImportPath);
                var dwcCollectionArchiveReaderContext = ArchiveReaderContext.Create(archiveReader, dataProvider, _dwcaConfiguration);
                var dwcCollectionRepository = new DwcCollectionRepository(dataProvider, _verbatimClient, _logger);
                dwcCollectionRepository.BeginTempMode();
                _logger.LogDebug($"Clear DwC-A observations for {dataProvider.Identifier}");
                await dwcCollectionRepository.DeleteCollectionsAsync();

                // Read datasets
                List<Lib.Models.Processed.DataStewardship.Dataset.DwcVerbatimDataset> datasets = null!;
                try
                {
                    datasets = await _dwcArchiveReader.ReadDatasetsAsync(dwcCollectionArchiveReaderContext);
                } 
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reading DwC-A datasets for {dataProvider.Identifier}");
                }

                // Read observations
                _logger.LogDebug($"Start storing DwC-A observations for {dataProvider.Identifier}");
                await dwcCollectionRepository.OccurrenceRepository.AddCollectionAsync();
                int observationCount = 0;
                var observationBatches = _dwcArchiveReader.ReadOccurrencesInBatchesAsync(dwcCollectionArchiveReaderContext);
                await foreach (var verbatimObservationsBatch in observationBatches)
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                    if (_dwcaConfiguration.MaxNumberOfSightingsHarvested.HasValue &&
                        observationCount >= _dwcaConfiguration.MaxNumberOfSightingsHarvested)
                    {
                        _logger.LogInformation($"Max observations for {dataProvider.Identifier} reached");
                        break;
                    }

                    observationCount += verbatimObservationsBatch.Count();
                    await dwcCollectionRepository.OccurrenceRepository.AddManyAsync(verbatimObservationsBatch);
                }
                await dwcCollectionRepository.OccurrenceRepository.PermanentizeCollectionAsync();
                _logger.LogDebug($"Finish storing DwC-A observations for {dataProvider.Identifier}");
                

                // Read events
                try
                { 
                    var events = (await _dwcArchiveReader.ReadEventsAsync(dwcCollectionArchiveReaderContext))?.ToList();
                    if (events != null && events.Any())
                    {
                        observationCount += await CreateAndStoreAbsentObservations(dataProvider, dwcCollectionRepository, events);

                        _logger.LogDebug($"Start storing DwC-A events for {dataProvider.Identifier}");
                        await dwcCollectionRepository.EventRepository.AddCollectionAsync();
                        await dwcCollectionRepository.EventRepository.AddManyAsync(events);
                        await dwcCollectionRepository.EventRepository.PermanentizeCollectionAsync();
                        _logger.LogDebug($"Finish storing DwC-A events for {dataProvider.Identifier}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reading DwC-A events for {dataProvider.Identifier}");
                }

                // Save datasets lasts, since DataSet.EventIds could been changed after reading Events
                try
                {                    
                    if (datasets != null && datasets.Any())
                    {
                        _logger.LogDebug($"Start storing DwC-A datasets for {dataProvider.Identifier}");
                        await dwcCollectionRepository.DatasetRepository.AddCollectionAsync();
                        await dwcCollectionRepository.DatasetRepository.AddManyAsync(datasets);
                        await dwcCollectionRepository.DatasetRepository.PermanentizeCollectionAsync();
                        _logger.LogDebug($"Finish storing DwC-A datasets for {dataProvider.Identifier}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reading DwC-A datasets for {dataProvider.Identifier}");
                }

                dwcCollectionRepository.EndTempMode();

                if (dataProvider.UseVerbatimFileInExport)
                {
                    _logger.LogDebug($"Start storing source file for {dataProvider.Identifier}");
                    await using var fileStream = File.OpenRead(archivePath);
                    await dwcArchiveVerbatimRepository.StoreSourceFileAsync(dataProvider.Id, fileStream);
                    _logger.LogDebug($"Finish storing source file for {dataProvider.Identifier}");
                }
                
                // Update harvest info
                harvestInfo.End = DateTime.Now;
                harvestInfo.Status = RunStatus.Success;
                harvestInfo.Count = observationCount;                
            }
            catch (JobAbortedException e)
            {
                _logger.LogError(e, $"Canceled harvest of DwC Archive for {dataProvider.Identifier}");
                harvestInfo.Status = RunStatus.Canceled;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed harvest of DwC Archive for {dataProvider.Identifier}");
                harvestInfo.Status = RunStatus.Failed;
            }

            return harvestInfo;
        }

        private async Task<int> CreateAndStoreAbsentObservations(DataProvider dataProvider, DwcCollectionRepository dwcCollectionRepository, List<DwcEventOccurrenceVerbatim>? events)
        {
            try
            {                
                _logger.LogDebug($"Start storing absent DwC-A occurrences for {dataProvider.Identifier}");
                int observationCount = 0;
                var batchAbsentObservations = new List<DwcObservationVerbatim>();
                var id = await dwcCollectionRepository.OccurrenceRepository.GetMaxIdAsync();
                _logger.LogDebug($"MaxId={id} before adding absent observations");
                dwcCollectionRepository.OccurrenceRepository.TempMode = false;
                for (int i = 0; i < events.Count; i++)
                {
                    DwcEventOccurrenceVerbatim? ev = events[i];
                    ev.Observations = null; // todo - handle this logic in the DwC-A parser.
                    var absentObservations = ev.CreateAbsentObservations();
                    foreach (var observation in absentObservations)
                    {
                        observation.Id = ++id;
                    }
                    batchAbsentObservations.AddRange(absentObservations);

                    // store batch of absent observations if this is the last iterated event or batch is larger than 10 000.
                    if (i == events.Count - 1 || batchAbsentObservations.Count > 10000)
                    {
                        observationCount += batchAbsentObservations.Count();
                        await dwcCollectionRepository.OccurrenceRepository.AddManyAsync(batchAbsentObservations);
                        batchAbsentObservations.Clear();
                    }
                }

                _logger.LogDebug($"Finish storing absent DwC-A occurrences for {dataProvider.Identifier}");
                return observationCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error storing absent observations for {dataProvider.Identifier}");
            }
            finally
            {
                dwcCollectionRepository.OccurrenceRepository.TempMode = true;                
            }

            return 0;
        }



        /// inheritdoc />
        public async Task<HarvestInfo> HarvestObservationsAsync(IJobCancellationToken cancellationToken)
        {
            throw new NotImplementedException("Not implemented for DwcA files");
        }

        /// inheritdoc />
        public async Task<HarvestInfo> HarvestObservationsAsync(JobRunModes mode,
            DateTime? fromDate,
            IJobCancellationToken cancellationToken)
        {
            throw new NotImplementedException("Not implemented for DwcA files");
        }

        private async Task<int?> GetNumberOfObservationsInExistingCollectionAsync(DataProvider dataProvider)
        {
            using var dwcArchiveVerbatimRepository = new DarwinCoreArchiveVerbatimRepository(
                    dataProvider,
                    _verbatimClient,
                    _logger)
            { TempMode = false };

            try
            {
                bool collectionExists = await dwcArchiveVerbatimRepository.CheckIfCollectionExistsAsync();
                if (!collectionExists) return null;
                long count = await dwcArchiveVerbatimRepository.CountAllDocumentsAsync();
                return Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed count number of observations for {dataProvider.Identifier}");
                return null;
            }            
        }

        /// inheritdoc />
        public async Task<HarvestInfo> HarvestObservationsAsync(DataProvider provider, IJobCancellationToken cancellationToken)
        {
            var harvestInfo = new HarvestInfo(provider.Identifier, DateTime.Now)
            {
                Status = RunStatus.Failed
            };
            XDocument emlDocument = null;
            _logger.LogInformation($"Start harvesting sightings for {provider.Identifier} data provider.");

            var downloadUrlEml = provider.DownloadUrls
                ?.FirstOrDefault(p => p.Type.Equals(DownloadUrl.DownloadType.ObservationEml))?.Url;

            if (!string.IsNullOrEmpty(downloadUrlEml))
            {
                try
                {
                    // Try to get eml document from ipt
                    emlDocument = await _fileDownloadService.GetXmlFileAsync(downloadUrlEml);

                    if (emlDocument != null)
                    {
                        if (DateTime.TryParse(
                            emlDocument.Root.Element("dataset").Element("pubDate").Value,
                            out var pubDate))
                        {
                            // If dataset not has changed since last harvest and there exist observations in MongoDB, don't harvest again
                            if (provider.SourceDate == pubDate.ToUniversalTime() && !_dwcaConfiguration.ForceHarvestUnchangedDwca)
                            {
                                var nrExistingObservations = await GetNumberOfObservationsInExistingCollectionAsync(provider);
                                if (nrExistingObservations.GetValueOrDefault() > 0)
                                {
                                    _logger.LogInformation($"Harvest of {provider.Identifier} canceled, No new data");
                                    harvestInfo.Status = RunStatus.CanceledSuccess;
                                    return harvestInfo;
                                }
                            }
                            
                            provider.SourceDate = pubDate;
                        };
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error getting EML file for {provider.Identifier}");
                }
            }

            var path = Path.Combine(_dwcaConfiguration.ImportPath, $"dwca-{provider.Identifier}.zip");

            // Try to get DwcA file from IPT and store it locally
            var downloadUrl = provider.DownloadUrls
                ?.FirstOrDefault(p => p.Type.Equals(DownloadUrl.DownloadType.Observations))?.Url;
            if (!await _fileDownloadService.GetFileAndStoreAsync(downloadUrl, path))
            {
                return harvestInfo;
            }

            // Harvest file
            harvestInfo = await HarvestObservationsAsync(path, provider, cancellationToken);

            if (harvestInfo.Status == RunStatus.Success && emlDocument != null)
            {
                if (!await _dataProviderRepository.StoreEmlAsync(provider.Id, emlDocument))
                {
                    _logger.LogWarning($"Error updating EML for {provider.Identifier}");
                }
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _logger.LogInformation($"Finish harvesting sightings for {provider.Identifier} data provider. Status={harvestInfo.Status}");
            return harvestInfo;
        }
    }
}