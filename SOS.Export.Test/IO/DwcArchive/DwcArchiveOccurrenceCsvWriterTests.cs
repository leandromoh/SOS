﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Moq;
using Newtonsoft.Json;
using SOS.Export.Enums;
using SOS.Export.Factories;
using SOS.Export.Helpers;
using SOS.Export.IO.DwcArchive;
using SOS.Export.Repositories.Interfaces;
using SOS.Export.Services;
using SOS.Export.Services.Interfaces;
using SOS.Export.Test.TestHelpers.JsonConverters;
using SOS.Export.Test.TestHelpers.Stubs;
using SOS.Lib.Configuration.Export;
using SOS.Lib.Models.DarwinCore;
using Xunit;

namespace SOS.Export.Test.IO.DwcArchive
{
    // todo - create DarwinCore class that is used for reading data where all properties is of string class? Suggested names: FlatDwcObservation, CsvDwcObservation, 
    public class DwcArchiveOccurrenceCsvWriterTests : TestBase
    {
        private readonly DwcArchiveOccurrenceCsvWriter _dwcArchiveOccurrenceCsvWriter;

        public DwcArchiveOccurrenceCsvWriterTests()
        {
            _dwcArchiveOccurrenceCsvWriter = new DwcArchiveOccurrenceCsvWriter(
                new Mock<ILogger<DwcArchiveOccurrenceCsvWriter>>().Object);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "DwcArchiveUnit")]
        public async Task CreateDwcOccurrenceCsvFile_LoadTenObservations_ShouldSucceed()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var processedDarwinCoreRepositoryMock = CreateProcessedDarwinCoreRepositoryMock(@"Resources\TenProcessedTestObservations.json");
            var memoryStream = new MemoryStream();

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            bool result = await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                memoryStream, 
                FieldDescriptionHelper.GetDefaultDwcExportFieldDescriptions(),
                processedDarwinCoreRepositoryMock.Object, 
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            List<dynamic> records = ReadCsvFile(memoryStream).ToList();
            result.Should().BeTrue();
            records.Count.Should().Be(10);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "DwcArchiveUnit")]
        public async Task CreateDwcOccurrenceCsvFile_LongLatProperties_ShouldBeRoundedToFiveDecimals()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var memoryStream = new MemoryStream();
            var observation = GetDefaultObservation();
            observation.Location.DecimalLatitude = 13.823392373018132;
            observation.Location.DecimalLongitude = 55.51071440795833;
            var processedDarwinCoreRepositoryMock = CreateProcessedDarwinCoreRepositoryMock(observation);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                memoryStream,
                FieldDescriptionHelper.GetDefaultDwcExportFieldDescriptions(),
                processedDarwinCoreRepositoryMock.Object,
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            dynamic record = ReadCsvFile(memoryStream).First();
            ((string)record.decimalLatitude).Should().Be("13.82339");
            ((string)record.decimalLongitude).Should().Be("55.51071");
        }


        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "DwcArchiveUnit")]
        public async Task CreateDwcOccurrenceCsvFile_CoordinateUncertaintyProperty_ShouldNeverBeZero()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var memoryStream = new MemoryStream();
            var observation = GetDefaultObservation();
            observation.Location.CoordinateUncertaintyInMeters = 0;
            var processedDarwinCoreRepositoryMock = CreateProcessedDarwinCoreRepositoryMock(observation);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                memoryStream,
                FieldDescriptionHelper.GetDefaultDwcExportFieldDescriptions(),
                processedDarwinCoreRepositoryMock.Object,
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            dynamic record = ReadCsvFile(memoryStream).First();
            ((string)record.coordinateUncertaintyInMeters).Should().NotBe("0", "because Zero is not valid value for this term (https://dwc.tdwg.org/terms/#dwc:coordinateUncertaintyInMeters)");
            ((string)record.coordinateUncertaintyInMeters).Should().Be("1", "because this is the closest natural number to 0");
        }


        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "DwcArchiveUnit")]
        public async Task CreateDwcOccurrenceCsvFile_OccurrenceRemarksWithNewLine_ShouldBeReplacedBySpace()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var memoryStream = new MemoryStream();
            var observation = GetDefaultObservation();
            observation.Occurrence.OccurrenceRemarks = "Sighting found in\r\nUppsala";
            var processedDarwinCoreRepositoryMock = CreateProcessedDarwinCoreRepositoryMock(observation);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                memoryStream,
                FieldDescriptionHelper.GetDefaultDwcExportFieldDescriptions(),
                processedDarwinCoreRepositoryMock.Object,
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            dynamic record = ReadCsvFile(memoryStream).First();
            ((string)record.occurrenceRemarks).Should().Be("Sighting found in Uppsala");
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "DwcArchiveUnit")]
        public async Task CreateDwcOccurrenceCsvFile_WithSubsetOfFieldDescriptions_OnlySpecifiedFieldDescriptionsShouldBeUsed()
        {
            //-----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            var memoryStream = new MemoryStream();
            var observation = GetDefaultObservation();
            var processedDarwinCoreRepositoryMock = CreateProcessedDarwinCoreRepositoryMock(observation);
            List<FieldDescriptionId> fieldDescriptionIds = new List<FieldDescriptionId>
            {
                FieldDescriptionId.OccurrenceID,
                FieldDescriptionId.ScientificName,
                FieldDescriptionId.DecimalLongitude,
                FieldDescriptionId.DecimalLatitude
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            await _dwcArchiveOccurrenceCsvWriter.CreateOccurrenceCsvFileAsync(
                memoryStream,
                FieldDescriptionHelper.GetFieldDescriptions(fieldDescriptionIds),
                processedDarwinCoreRepositoryMock.Object,
                JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            dynamic record = ReadCsvFile(memoryStream).First();
            var recordDictionary = (IDictionary<string, object>) record;
            recordDictionary.Should().ContainKey("occurrenceID", "because this field was provided as field description");
            recordDictionary.Should().NotContainKey("basisOfRecord", "because this field was not provided as field description");
        }


        private static List<dynamic> ReadCsvFile(MemoryStream memoryStream)
        {
            using var readMemoryStream = new MemoryStream(memoryStream.ToArray());
            using var streamReader = new StreamReader(readMemoryStream);
            using var csvReader = new CsvReader(streamReader);
            SetCsvConfigurations(csvReader);
            var records = csvReader.GetRecords<dynamic>().ToList();
            return records;
        }

        private static void SetCsvConfigurations(CsvReader csv)
        {
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.Delimiter = "\t";
            csv.Configuration.Encoding = System.Text.Encoding.UTF8;
            csv.Configuration.BadDataFound = x => { Console.WriteLine($"Bad data: <{x.RawRecord}>"); };
        }
    }
}