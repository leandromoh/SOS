﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Ionic.Zip;
using Microsoft.Extensions.Logging;
using SOS.Export.IO.DwcArchive.Interfaces;
using SOS.Export.Models;
using SOS.Export.Repositories.Interfaces;
using SOS.Export.Services.Interfaces;
using SOS.Lib.Helpers;
using SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Processed.ProcessInfo;
using SOS.Lib.Models.Search;

namespace SOS.Export.IO.DwcArchive
{
    public class DwcArchiveFileWriter : IDwcArchiveFileWriter
    {
        private readonly IDwcArchiveOccurrenceCsvWriter _dwcArchiveOccurrenceCsvWriter;
        private readonly IExtendedMeasurementOrFactCsvWriter _extendedMeasurementOrFactCsvWriter;
        private readonly IFileService _fileService;
        private readonly ILogger<DwcArchiveFileWriter> _logger;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="dwcArchiveOccurrenceCsvWriter"></param>
        /// <param name="extendedMeasurementOrFactCsvWriter"></param>
        /// <param name="fileService"></param>
        /// <param name="logger"></param>
        public DwcArchiveFileWriter(
            IDwcArchiveOccurrenceCsvWriter dwcArchiveOccurrenceCsvWriter,
            IExtendedMeasurementOrFactCsvWriter extendedMeasurementOrFactCsvWriter,
            IFileService fileService,
            ILogger<DwcArchiveFileWriter> logger)
        {
            _dwcArchiveOccurrenceCsvWriter = dwcArchiveOccurrenceCsvWriter ??
                                             throw new ArgumentNullException(nameof(dwcArchiveOccurrenceCsvWriter));
            _extendedMeasurementOrFactCsvWriter = extendedMeasurementOrFactCsvWriter ??
                                                  throw new ArgumentNullException(
                                                      nameof(extendedMeasurementOrFactCsvWriter));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string> CreateDwcArchiveFileAsync(
            FilterBase filter,
            string fileName,
            IProcessedObservationRepository processedObservationRepository,
            ProcessInfo processInfo,
            string exportFolderPath,
            IJobCancellationToken cancellationToken)
        {
            IEnumerable<FieldDescription> fieldDescriptions = FieldDescriptionHelper.GetDwcFieldDescriptionsForTestingPurpose();

            return await CreateDwcArchiveFileAsync(
                filter,
                fileName,
                processedObservationRepository,
                fieldDescriptions,
                processInfo,
                exportFolderPath,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> CreateDwcArchiveFileAsync(
            FilterBase filter,
            string fileName,
            IProcessedObservationRepository processedObservationRepository,
            IEnumerable<FieldDescription> fieldDescriptions,
            ProcessInfo processInfo,
            string exportFolderPath,
            IJobCancellationToken cancellationToken)
        {
            string temporaryZipExportFolderPath = null;

            try
            {
                temporaryZipExportFolderPath = Path.Combine(exportFolderPath, fileName);
                _fileService.CreateFolder(temporaryZipExportFolderPath);
                var occurrenceCsvFilePath = Path.Combine(temporaryZipExportFolderPath, "occurrence.csv");
                var emofCsvFilePath = Path.Combine(temporaryZipExportFolderPath, "extendedMeasurementOrFact.csv");
                var metaXmlFilePath = Path.Combine(temporaryZipExportFolderPath, "meta.xml");
                var emlXmlFilePath = Path.Combine(temporaryZipExportFolderPath, "eml.xml");
                var processInfoXmlFilePath = Path.Combine(temporaryZipExportFolderPath, "processinfo.xml");

                // Create Occurrence.csv
                using (var fileStream = File.Create(occurrenceCsvFilePath, 128 * 1024))
                {
                    await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                        filter,
                        fileStream,
                        fieldDescriptions,
                        processedObservationRepository,
                        cancellationToken);
                }

                // Create ExtendedMeasurementOrFact.csv
                using (var fileStream = File.Create(emofCsvFilePath))
                {
                    await _extendedMeasurementOrFactCsvWriter.CreateCsvFileAsync(
                        filter,
                        fileStream,
                        fieldDescriptions,
                        processedObservationRepository,
                        cancellationToken);
                }

                // Create meta.xml
                using (var fileStream = File.Create(metaXmlFilePath))
                {
                    DwcArchiveMetaFileWriter.CreateMetaXmlFile(fileStream, fieldDescriptions.ToList());
                }

                // Create eml.xml
                using (var fileStream = File.Create(emlXmlFilePath))
                {
                    await DwCArchiveEmlFileFactory.CreateEmlXmlFileAsync(fileStream);
                }

                // Create processinfo.xml
                await using var processInfoFileStream = File.Create(processInfoXmlFilePath);
                DwcProcessInfoFileWriter.CreateProcessInfoFile(processInfoFileStream, processInfo);
                processInfoFileStream.Close();
                var zipFilePath = _fileService.CompressFolder(exportFolderPath, fileName);
                _fileService.DeleteFolder(temporaryZipExportFolderPath);
                return zipFilePath;
            }
            catch (JobAbortedException)
            {
                _logger.LogInformation("CreateDwcArchiveFile was canceled.");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to create Dwc Archive File.");
                throw;
            }
            finally
            {
                _fileService.DeleteFolder(temporaryZipExportFolderPath);
            }
        }

        public async Task WriteObservations(
            IEnumerable<DarwinCore> dwcObservations,
            Dictionary<DwcaFilePart, string> filePathByFilePart)
        {
            string occurrenceCsvFilePath = filePathByFilePart[DwcaFilePart.Occurrence];
            var fieldDescriptions = FieldDescriptionHelper.GetDwcFieldDescriptionsForTestingPurpose();
            
            // Create Occurrence CSV file
            await using StreamWriter fileStream = File.AppendText(occurrenceCsvFilePath);
            await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                dwcObservations,
                fileStream,
                fieldDescriptions);

            // Create EMOF CSV file
            // todo

            // Create Multimedia CSV file
            // todo
        }

        public async Task CreateDwcArchiveFileAsync(DwcaFilesCreationInfo dwcaFileCreationInfo)
        {
            await using var stream = File.Create(@"c:\temp\minfil.zip");
            await using var compressedFileStream = new ZipOutputStream(stream, true);
            compressedFileStream.EnableZip64 = Zip64Option.AsNecessary;
            
            compressedFileStream.PutNextEntry("occurrence.csv");
            await WriteOccurrenceHeader(compressedFileStream);
            foreach (var value in dwcaFileCreationInfo.FilePathByBatchIdAndFilePart.Values)
            {
                string occurrenceCsvFilePath = value[DwcaFilePart.Occurrence];
                await using var readStream = File.OpenRead(occurrenceCsvFilePath);
                await readStream.CopyToAsync(compressedFileStream);
            }
        }

        private async Task WriteOccurrenceHeader(ZipOutputStream compressedFileStream)
        {
            await using var streamWriter = new StreamWriter(compressedFileStream, Encoding.UTF8, -1, true);
            var csvWriter = new NReco.Csv.CsvWriter(streamWriter, "\t");
            _dwcArchiveOccurrenceCsvWriter.WriteHeaderRow(csvWriter,
                FieldDescriptionHelper.GetDwcFieldDescriptionsForTestingPurpose());
            await streamWriter.FlushAsync();
        }
    }
}