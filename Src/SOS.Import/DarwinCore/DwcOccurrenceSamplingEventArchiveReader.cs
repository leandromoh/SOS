﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DwC_A;
using DwC_A.Terms;
using Microsoft.Extensions.Logging;
using SOS.Import.DarwinCore.Factories;
using SOS.Import.DarwinCore.Interfaces;
using SOS.Lib.Models.Interfaces;
using SOS.Lib.Models.Verbatim.DarwinCore;

namespace SOS.Import.DarwinCore
{
    /// <summary>
    ///     DwC-A reader for sampling event based DwC-A as DwcObservationVerbatim collection.
    /// </summary>
    public class DwcOccurrenceSamplingEventArchiveReader : IDwcArchiveReaderAsDwcObservation
    {
        private readonly ILogger<DwcArchiveReader> _logger;
        private int _idCounter;
        protected int NextId => Interlocked.Increment(ref _idCounter);

        public DwcOccurrenceSamplingEventArchiveReader(ILogger<DwcArchiveReader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _idCounter = 0;
        }

        /// <summary>
        ///     Reads a sampling event based DwC-A, and returns observations in batches.
        /// </summary>
        /// <param name="archiveReader"></param>
        /// <param name="idIdentifierTuple"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<List<DwcObservationVerbatim>> ReadArchiveInBatchesAsync(
            ArchiveReader archiveReader,
            IIdIdentifierTuple idIdentifierTuple,
            int batchSize)
        {
            var occurrenceFileReader = archiveReader.GetAsyncFileReader(RowTypes.Occurrence);
            if (occurrenceFileReader == null) yield break;
            var occurrenceRecords = new List<DwcObservationVerbatim>();
            var idIndex = occurrenceFileReader.GetIdIndex();

            await foreach (var row in occurrenceFileReader.GetDataRowsAsync())
            {
                var occurrenceRecord = DwcObservationVerbatimFactory.Create(NextId, row, idIdentifierTuple, idIndex);
                occurrenceRecords.Add(occurrenceRecord);

                if (occurrenceRecords.Count % batchSize == 0)
                {
                    await AddDataFromExtensionsAsync(archiveReader, occurrenceRecords);
                    yield return occurrenceRecords;
                    occurrenceRecords.Clear();
                }
            }

            await AddDataFromExtensionsAsync(archiveReader, occurrenceRecords);
            yield return occurrenceRecords;
        }

        public async Task<List<DwcObservationVerbatim>> ReadArchiveAsync(
            ArchiveReader archiveReader,
            IIdIdentifierTuple idIdentifierTuple)
        {
            const int batchSize = 100000;
            var observationsBatches = ReadArchiveInBatchesAsync(
                archiveReader,
                idIdentifierTuple,
                batchSize);
            var observations = new List<DwcObservationVerbatim>();
            await foreach (var observationsBatch in observationsBatches)
            {
                observations.AddRange(observationsBatch);
            }

            return observations;
        }


        /// <summary>
        ///     Add data from DwC-A extensions
        /// </summary>
        /// <param name="archiveReader"></param>
        /// <param name="occurrenceRecords"></param>
        /// <returns></returns>
        private async Task AddDataFromExtensionsAsync(ArchiveReader archiveReader,
            List<DwcObservationVerbatim> occurrenceRecords)
        {
            await AddEventDataAsync(occurrenceRecords, archiveReader);
            await AddEmofExtensionDataAsync(occurrenceRecords, archiveReader);
            await AddMofExtensionDataAsync(occurrenceRecords, archiveReader);
            await AddMultimediaExtensionDataAsync(occurrenceRecords, archiveReader);
            await AddAudubonMediaExtensionDataAsync(occurrenceRecords, archiveReader);
        }

        /// <summary>
        ///     Add Measurement Or Fact extension data
        /// </summary>
        /// <param name="occurrenceRecords"></param>
        /// <param name="archiveReader"></param>
        /// <returns></returns>
        private async Task AddMofExtensionDataAsync(List<DwcObservationVerbatim> occurrenceRecords,
            ArchiveReader archiveReader)
        {
            try
            {
                var mofFileReader = archiveReader.GetAsyncFileReader(RowTypes.MeasurementOrFact);
                if (mofFileReader == null) return;
                var idIndex = mofFileReader.GetIdIndex();

                var observationsByRecordId =
                    occurrenceRecords
                        .GroupBy(observation => observation.RecordId)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

                await foreach (var row in mofFileReader.GetDataRowsAsync())
                {
                    var id = row[idIndex];
                    AddEventMof(row, id, observationsByRecordId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add MeasurementOrFact extension data");
                throw;
            }
        }

        /// <summary>
        ///     Add Simple Multimedia extension data
        /// </summary>
        /// <param name="occurrenceRecords"></param>
        /// <param name="archiveReader"></param>
        /// <returns></returns>
        private async Task AddMultimediaExtensionDataAsync(List<DwcObservationVerbatim> occurrenceRecords,
            ArchiveReader archiveReader)
        {
            try
            {
                var multimediaFileReader = archiveReader.GetAsyncFileReader(RowTypes.Multimedia);
                if (multimediaFileReader == null) return;
                var idIndex = multimediaFileReader.GetIdIndex();
                var observationsByRecordId =
                    occurrenceRecords
                        .GroupBy(observation => observation.RecordId)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

                await foreach (var row in multimediaFileReader.GetDataRowsAsync())
                {
                    var id = row[idIndex];
                    if (!observationsByRecordId.TryGetValue(id, out var observations)) continue;
                    foreach (var observation in observations)
                    {
                        if (observation.EventMultimedia == null)
                        {
                            observation.EventMultimedia = new List<DwcMultimedia>();
                        }

                        var multimediaItem = DwcMultimediaFactory.Create(row);
                        observation.EventMultimedia.Add(multimediaItem);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add Multimedia extension data");
                throw;
            }
        }

        private async Task AddAudubonMediaExtensionDataAsync(List<DwcObservationVerbatim> occurrenceRecords,
            ArchiveReader archiveReader)
        {
            try
            {
                var audubonFileReader = archiveReader.GetAsyncFileReader(RowTypes.AudubonMediaDescription);
                if (audubonFileReader == null) return;
                var idIndex = audubonFileReader.GetIdIndex();
                var observationsByRecordId =
                    occurrenceRecords
                        .GroupBy(observation => observation.RecordId)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

                await foreach (var row in audubonFileReader.GetDataRowsAsync())
                {
                    var id = row[idIndex];
                    if (!observationsByRecordId.TryGetValue(id, out var observations)) continue;
                    foreach (var observation in observations)
                    {
                        if (observation.EventAudubonMedia == null)
                        {
                            observation.EventAudubonMedia = new List<DwcAudubonMedia>();
                        }

                        var multimediaItem = DwcAudubonMediaFactory.Create(row);
                        observation.EventAudubonMedia.Add(multimediaItem);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add Audubon media description extension data");
                throw;
            }
        }

        /// <summary>
        ///     Add MeasureMentOrFact data
        /// </summary>
        /// <param name="row"></param>
        /// <param name="id"></param>
        /// <param name="observationsByRecordId"></param>
        private void AddEventMof(
            IRow row,
            string id,
            Dictionary<string, IEnumerable<DwcObservationVerbatim>> observationsByRecordId)
        {
            if (!observationsByRecordId.TryGetValue(id, out var observations)) return;
            foreach (var observation in observations)
            {
                if (observation.EventMeasurementOrFacts == null)
                {
                    observation.EventMeasurementOrFacts = new List<DwcMeasurementOrFact>();
                }

                var mofItem = DwcMeasurementOrFactFactory.Create(row);
                observation.EventMeasurementOrFacts.Add(mofItem);
            }
        }

        /// <summary>
        ///     Add event data to observations by reading the core file.
        /// </summary>
        /// <param name="occurrenceRecords"></param>
        /// <param name="archiveReader"></param>
        /// <returns></returns>
        private async Task AddEventDataAsync(List<DwcObservationVerbatim> occurrenceRecords,
            ArchiveReader archiveReader)
        {
            var eventFileReader = archiveReader.GetAsyncCoreFile();
            var idIndex = eventFileReader.GetIdIndex();
            var observationsByRecordId =
                occurrenceRecords
                    .GroupBy(observation => observation.RecordId)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

            await foreach (var row in eventFileReader.GetDataRowsAsync())
            {
                var id = row[idIndex];
                if (!observationsByRecordId.TryGetValue(id, out var observations)) continue;
                foreach (var observation in observations)
                {
                    foreach (var fieldType in row.FieldMetaData)
                    {
                        var val = row[fieldType.Index];
                        DwcTermValueMapper.MapValueByTerm(observation, fieldType.Term, val);
                    }
                }
            }
        }

        /// <summary>
        ///     Add Extended Measurement Or Fact data
        /// </summary>
        /// <param name="occurrenceRecords"></param>
        /// <param name="archiveReader"></param>
        /// <returns></returns>
        private async Task AddEmofExtensionDataAsync(List<DwcObservationVerbatim> occurrenceRecords,
            ArchiveReader archiveReader)
        {
            try
            {
                var emofFileReader = archiveReader.GetAsyncFileReader(RowTypes.ExtendedMeasurementOrFact);
                if (emofFileReader == null) return;
                var idIndex = emofFileReader.GetIdIndex();
                var occurrenceIdFieldMetaData = emofFileReader.TryGetFieldMetaData(Terms.occurrenceID);
                var observationsByRecordId =
                    occurrenceRecords
                        .GroupBy(observation => observation.RecordId)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

                if (occurrenceIdFieldMetaData == null
                ) // If there is no occurrenceID field, then add only event measurements
                {
                    await foreach (var row in emofFileReader.GetDataRowsAsync())
                    {
                        var id = row[idIndex];
                        AddEventEmof(row, id, observationsByRecordId);
                    }
                }
                else // occurrenceID field exist, try to get both occurrence measurements and event measurements
                {
                    var observationByOccurrenceId =
                        occurrenceRecords.ToDictionary(v => v.OccurrenceID, v => v);
                    await foreach (var row in emofFileReader.GetDataRowsAsync())
                    {
                        var occurrenceId = row[occurrenceIdFieldMetaData.Index];
                        AddOccurrenceEmof(row, occurrenceId, observationByOccurrenceId);
                        if (string.IsNullOrEmpty(occurrenceId)) // Measurement for event
                        {
                            var id = row[idIndex];
                            AddEventEmof(row, id, observationsByRecordId);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add ExtendedMeasurementOrFact extension data");
                throw;
            }
        }

        /// <summary>
        ///     Add event measurement
        /// </summary>
        /// <param name="row"></param>
        /// <param name="id"></param>
        /// <param name="observationsByRecordId"></param>
        private void AddEventEmof(
            IRow row,
            string id,
            Dictionary<string, IEnumerable<DwcObservationVerbatim>> observationsByRecordId)
        {
            if (!observationsByRecordId.TryGetValue(id, out var observations)) return;
            foreach (var observation in observations)
            {
                if (observation.EventExtendedMeasurementOrFacts == null)
                {
                    observation.EventExtendedMeasurementOrFacts = new List<DwcExtendedMeasurementOrFact>();
                }

                var emofItem = DwcExtendedMeasurementOrFactFactory.Create(row);
                observation.EventExtendedMeasurementOrFacts.Add(emofItem);
            }
        }

        /// <summary>
        ///     Add occurrence measurement.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="occurrenceId"></param>
        /// <param name="observationByOccurrenceId"></param>
        private void AddOccurrenceEmof(
            IRow row,
            string occurrenceId,
            Dictionary<string, DwcObservationVerbatim> observationByOccurrenceId)
        {
            if (string.IsNullOrEmpty(occurrenceId)) return;

            if (observationByOccurrenceId.TryGetValue(occurrenceId, out var obs))
            {
                if (obs.ObservationExtendedMeasurementOrFacts == null)
                {
                    obs.ObservationExtendedMeasurementOrFacts = new List<DwcExtendedMeasurementOrFact>();
                }

                var emofItem = DwcExtendedMeasurementOrFactFactory.Create(row);
                obs.ObservationExtendedMeasurementOrFacts.Add(emofItem);
            }
        }
    }
}