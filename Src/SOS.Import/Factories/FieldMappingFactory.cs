﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SOS.Import.Factories.FieldMappings;
using SOS.Import.Factories.FieldMappings.Interfaces;
using SOS.Import.Repositories.Destination.FieldMappings.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;

namespace SOS.Import.Factories
{
    /// <summary>
    /// Class for handling field mappings.
    /// </summary>
    public class FieldMappingFactory : Interfaces.IFieldMappingFactory { 

        private readonly IFieldMappingRepository _fieldMappingRepository;
        private readonly IGeoRegionFieldMappingFactory _geoRegionFieldMappingFactory;
        private readonly IActivityFieldMappingFactory _activityFieldMappingFactory;
        private readonly IGenderFieldMappingFactory _genderFieldMappingFactory;
        private readonly ILifeStageFieldMappingFactory _lifeStageFieldMappingFactory;
        private readonly IBiotopeFieldMappingFactory _biotopeFieldMappingFactory;
        private readonly ISubstrateFieldMappingFactory _substrateFieldMappingFactory;
        private readonly IValidationStatusFieldMappingFactory _validationStatusFieldMappingFactory;
        private readonly ILogger<FieldMappingFactory> _logger;
        private readonly Dictionary<FieldMappingFieldId, IFieldMappingCreatorFactory> _fieldMappingFactoryById;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fieldMappingRepository"></param>
        /// <param name="genderFieldMappingFactory"></param>
        /// <param name="lifeStageFieldMappingFactory"></param>
        /// <param name="logger"></param>
        /// <param name="geoRegionFieldMappingFactory"></param>
        /// <param name="activityFieldMappingFactory"></param>
        public FieldMappingFactory(
            IFieldMappingRepository fieldMappingRepository,
            IGeoRegionFieldMappingFactory geoRegionFieldMappingFactory,
            IActivityFieldMappingFactory activityFieldMappingFactory,
            IGenderFieldMappingFactory genderFieldMappingFactory,
            ILifeStageFieldMappingFactory lifeStageFieldMappingFactory,
            IBiotopeFieldMappingFactory biotopeFieldMappingFactory,
            ISubstrateFieldMappingFactory substrateFieldMappingFactory,
            IValidationStatusFieldMappingFactory validationStatusFieldMappingFactory,
            ILogger<FieldMappingFactory> logger)
        {
            _fieldMappingRepository = fieldMappingRepository ?? throw new ArgumentNullException(nameof(fieldMappingRepository));
            _geoRegionFieldMappingFactory = geoRegionFieldMappingFactory ?? throw new ArgumentNullException(nameof(geoRegionFieldMappingFactory));
            _activityFieldMappingFactory = activityFieldMappingFactory ?? throw new ArgumentNullException(nameof(activityFieldMappingFactory));
            _genderFieldMappingFactory = genderFieldMappingFactory ?? throw new ArgumentNullException(nameof(genderFieldMappingFactory));
            _lifeStageFieldMappingFactory = lifeStageFieldMappingFactory ?? throw new ArgumentNullException(nameof(lifeStageFieldMappingFactory));
            _biotopeFieldMappingFactory = biotopeFieldMappingFactory ?? throw new ArgumentNullException(nameof(biotopeFieldMappingFactory));
            _substrateFieldMappingFactory = substrateFieldMappingFactory ?? throw new ArgumentNullException(nameof(substrateFieldMappingFactory));
            _validationStatusFieldMappingFactory = validationStatusFieldMappingFactory ?? throw new ArgumentNullException(nameof(validationStatusFieldMappingFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _fieldMappingFactoryById = new Dictionary<FieldMappingFieldId, IFieldMappingCreatorFactory>
            {
                {FieldMappingFieldId.LifeStage, _lifeStageFieldMappingFactory},
                {FieldMappingFieldId.Activity, _activityFieldMappingFactory},
                {FieldMappingFieldId.Gender, _genderFieldMappingFactory},
                {FieldMappingFieldId.Biotope, _biotopeFieldMappingFactory},
                {FieldMappingFieldId.Substrate, _substrateFieldMappingFactory},
                {FieldMappingFieldId.ValidationStatus, _validationStatusFieldMappingFactory}
            };

        }

        /// <summary>
        /// Import field mappings.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ImportAsync()
        {
            try
            {
                _logger.LogDebug("Start importing field mappings");
                var fieldMappings = new List<FieldMapping>();
                foreach (string fileName in Directory.GetFiles(@"Resources\FieldMappings\"))
                {
                    var fieldMapping = CreateFieldMappingFromJsonFile(fileName);
                    fieldMappings.Add(fieldMapping);
                }
                fieldMappings = fieldMappings.OrderBy(m => m.Id).ToList();
                
                await _fieldMappingRepository.DeleteCollectionAsync();
                await _fieldMappingRepository.AddCollectionAsync();
                await _fieldMappingRepository.AddManyAsync(fieldMappings);
                _logger.LogDebug("Finish storing field mappings");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed importing field mappings");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<byte[]> CreateFieldMappingsZipFileAsync(IEnumerable<FieldMappingFieldId> fieldMappingFieldIds)
        {
            var fielMappingFileTuples = new List<(string Filename, byte[] Bytes)>();
            foreach (var fieldMappingFieldId in fieldMappingFieldIds)
            {
                fielMappingFileTuples.Add(await CreateFieldMappingFileAsync(fieldMappingFieldId));
            }

            byte[] zipFile = CreateZipFile(fielMappingFileTuples);
            return zipFile;
        }

        /// <inheritdoc />
        public async Task<(string Filename, byte[] Bytes)> CreateFieldMappingFileAsync(FieldMappingFieldId fieldMappingFieldId)
        {
            FieldMapping fieldMapping;
            string filename = $"{fieldMappingFieldId.ToString()}FieldMapping.json";
            switch (fieldMappingFieldId)
            {
                case FieldMappingFieldId.Activity:
                case FieldMappingFieldId.LifeStage:
                case FieldMappingFieldId.Gender:
                case FieldMappingFieldId.Biotope:
                case FieldMappingFieldId.Substrate:
                case FieldMappingFieldId.ValidationStatus:
                    var fieldMappingFactory = _fieldMappingFactoryById[fieldMappingFieldId];
                    fieldMapping = await fieldMappingFactory.CreateFieldMappingAsync();
                    break;

                case FieldMappingFieldId.County:
                case FieldMappingFieldId.Municipality:
                case FieldMappingFieldId.Province:
                case FieldMappingFieldId.Parish:
                    var fieldMappingDictionary = await _geoRegionFieldMappingFactory.CreateFieldMappingsAsync();
                    fieldMapping = fieldMappingDictionary[fieldMappingFieldId];
                    break;
                
                default:
                    throw new ArgumentException(
                        $"{MethodBase.GetCurrentMethod().Name}() does not support the value {fieldMappingFieldId}", nameof(fieldMappingFieldId));
            }

            return CreateFieldMappingFileResult(fieldMapping, filename);
        }

        private FieldMapping CreateFieldMappingFromJsonFile(string filename)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(assemblyPath, filename);
            string str = File.ReadAllText(filePath, Encoding.UTF8);
            var fieldMappings = JsonConvert.DeserializeObject<FieldMapping>(str);
            return fieldMappings;
        }

        private (string Filename, byte[] Bytes) CreateFieldMappingFileResult(FieldMapping fieldMapping, string fileName)
        {
            var bytes = SerializeToJsonArray(fieldMapping, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }, Formatting.Indented);
            return (Filename: fileName, Bytes: bytes);
        }

        private byte[] SerializeToJsonArray(object value, JsonSerializerSettings jsonSerializerSettings, Formatting formatting)
        {
            var result = JsonConvert.SerializeObject(value, formatting, jsonSerializerSettings);
            return Encoding.UTF8.GetBytes(result);
        }

        private byte[] CreateZipFile(IEnumerable<(string Filename, byte[] Bytes)> files)
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var zipEntry = archive.CreateEntry(file.Filename, CompressionLevel.Optimal);
                    using var zipStream = zipEntry.Open();
                    zipStream.Write(file.Bytes, 0, file.Bytes.Length);
                }
            }

            return ms.ToArray();
        }
    }
}