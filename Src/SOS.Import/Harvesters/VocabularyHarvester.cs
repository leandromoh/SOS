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
using SOS.Import.Factories.Vocabularies;
using SOS.Import.Factories.Vocabularies.Interfaces;
using SOS.Import.Harvesters.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Shared;
using SOS.Lib.Extensions;
using SOS.Lib.Repositories.Resource.Interfaces;

namespace SOS.Import.Harvesters
{
    /// <summary>
    ///     Class for handling vocabularies.
    /// </summary>
    public class VocabularyHarvester : IVocabularyHarvester
    {
        private readonly Dictionary<VocabularyId, IVocabularyFactory> _vocabularyFactoryById;

        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ILogger<VocabularyHarvester> _logger;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="vocabularyRepository"></param>
        /// <param name="genderVocabularyFactory"></param>
        /// <param name="lifeStageVocabularyFactory"></param>
        /// <param name="validationStatusVocabularyFactory"></param>
        /// <param name="institutionVocabularyFactory"></param>
        /// <param name="unitVocabularyFactory"></param>
        /// <param name="basisOfRecordVocabularyFactory"></param>
        /// <param name="continentVocabularyFactory"></param>
        /// <param name="parishVocabularyFactory"></param>
        /// <param name="establishmentMeansVocabularyFactory"></param>
        /// <param name="determinationMethodVocabularyFactory"></param>
        /// <param name="logger"></param>
        /// <param name="activityVocabularyFactory"></param>
        /// <param name="biotopeVocabularyFactory"></param>
        /// <param name="substrateVocabularyFactory"></param>
        /// <param name="countyVocabularyFactory"></param>
        /// <param name="municipalityVocabularyFactory"></param>
        /// <param name="provinceVocabularyFactory"></param>
        /// <param name="typeVocabularyFactory"></param>
        /// <param name="countryVocabularyFactory"></param>
        /// <param name="accessRightsVocabularyFactory"></param>
        /// <param name="occurrenceStatusVocabularyFactory"></param>
        public VocabularyHarvester(
            IVocabularyRepository vocabularyRepository,
            ActivityVocabularyFactory activityVocabularyFactory,
            GenderVocabularyFactory genderVocabularyFactory,
            LifeStageVocabularyFactory lifeStageVocabularyFactory,
            BiotopeVocabularyFactory biotopeVocabularyFactory,
            SubstrateVocabularyFactory substrateVocabularyFactory,
            ValidationStatusVocabularyFactory validationStatusVocabularyFactory,
            InstitutionVocabularyFactory institutionVocabularyFactory,
            UnitVocabularyFactory unitVocabularyFactory,
            BasisOfRecordVocabularyFactory basisOfRecordVocabularyFactory,
            ContinentVocabularyFactory continentVocabularyFactory,
            CountyVocabularyFactory countyVocabularyFactory,
            MunicipalityVocabularyFactory municipalityVocabularyFactory,
            ProvinceVocabularyFactory provinceVocabularyFactory,
            ParishVocabularyFactory parishVocabularyFactory,
            TypeVocabularyFactory typeVocabularyFactory,
            CountryVocabularyFactory countryVocabularyFactory,
            AccessRightsVocabularyFactory accessRightsVocabularyFactory,
            OccurrenceStatusVocabularyFactory occurrenceStatusVocabularyFactory,
            EstablishmentMeansVocabularyFactory establishmentMeansVocabularyFactory,
            AreaTypeVocabularyFactory areaTypeVocabularyFactory,
            DiscoveryMethodVocabularyFactory discoveryMethodVocabularyFactory,
            DeterminationMethodVocabularyFactory determinationMethodVocabularyFactory,
            ILogger<VocabularyHarvester> logger)
        {
            _vocabularyRepository =
                vocabularyRepository ?? throw new ArgumentNullException(nameof(vocabularyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vocabularyFactoryById = new Dictionary<VocabularyId, IVocabularyFactory>
            {
                {VocabularyId.LifeStage, lifeStageVocabularyFactory},
                {VocabularyId.Activity, activityVocabularyFactory},
                {VocabularyId.Gender, genderVocabularyFactory},
                {VocabularyId.Biotope, biotopeVocabularyFactory},
                {VocabularyId.Substrate, substrateVocabularyFactory},
                {VocabularyId.ValidationStatus, validationStatusVocabularyFactory},
                {VocabularyId.Institution, institutionVocabularyFactory},
                {VocabularyId.Unit, unitVocabularyFactory},
                {VocabularyId.BasisOfRecord, basisOfRecordVocabularyFactory},
                {VocabularyId.Continent, continentVocabularyFactory},
                {VocabularyId.County, countyVocabularyFactory},
                {VocabularyId.Municipality, municipalityVocabularyFactory},
                {VocabularyId.Province, provinceVocabularyFactory},
                {VocabularyId.Parish, parishVocabularyFactory},
                {VocabularyId.Type, typeVocabularyFactory},
                {VocabularyId.Country, countryVocabularyFactory},
                {VocabularyId.AccessRights, accessRightsVocabularyFactory},
                {VocabularyId.OccurrenceStatus, occurrenceStatusVocabularyFactory},
                {VocabularyId.EstablishmentMeans, establishmentMeansVocabularyFactory},
                {VocabularyId.AreaType, areaTypeVocabularyFactory},
                {VocabularyId.DiscoveryMethod, discoveryMethodVocabularyFactory},
                {VocabularyId.DeterminationMethod, determinationMethodVocabularyFactory}
            };
        }

        /// <summary>
        ///     Import field mappings.
        /// </summary>
        /// <returns></returns>
        public async Task<HarvestInfo> HarvestAsync()
        {
            var harvestInfo = new HarvestInfo(nameof(Vocabulary), DataProviderType.Vocabularies, DateTime.Now);
            var vocabularies = new List<Vocabulary>();
            try
            {
                _logger.LogDebug("Start importing field mappings");

                foreach (var fileName in Directory.GetFiles(@"Resources\Vocabularies\"))
                {
                    var vocabulary = CreateVocabularyFromJsonFile(fileName);
                    vocabularies.Add(vocabulary);
                }

                vocabularies = vocabularies.OrderBy(m => m.Id).ToList();

                await _vocabularyRepository.DeleteCollectionAsync();
                await _vocabularyRepository.AddCollectionAsync();
                await _vocabularyRepository.AddManyAsync(vocabularies);
                _logger.LogDebug("Finish storing vocabularies");
                harvestInfo.Status = RunStatus.Success;
                harvestInfo.Count = vocabularies.Count;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed importing vocabularies");
                harvestInfo.Status = RunStatus.Failed;
            }

            harvestInfo.End = DateTime.Now;
            return harvestInfo;
        }

        /// <inheritdoc />
        public async Task<byte[]> CreateVocabulariesZipFileAsync(IEnumerable<VocabularyId> vocabularyIds)
        {
            var fielMappingFileTuples = new List<(string Filename, byte[] Bytes)>();
            foreach (var vocabularyId in vocabularyIds)
            {
                fielMappingFileTuples.Add(await CreateVocabularyFileAsync(vocabularyId));
            }

            var zipFile = CreateZipFile(fielMappingFileTuples);
            return zipFile;
        }

        public async Task<IEnumerable<Vocabulary>> CreateAllVocabulariesAsync(
            IEnumerable<VocabularyId> vocabularyIds)
        {
            var vocabularies = new List<Vocabulary>();
            foreach (var vocabularyId in vocabularyIds)
            {
                var vocabulary = await CreateVocabularyAsync(vocabularyId);
                vocabularies.Add(vocabulary);
            }

            return vocabularies;
        }

        /// <inheritdoc />
        public async Task<(string Filename, byte[] Bytes)> CreateVocabularyFileAsync(
            VocabularyId vocabularyId)
        {
            var filename = $"{vocabularyId}Vocabulary.json";
            var vocabulary = await CreateVocabularyAsync(vocabularyId);
            return CreateVocabularyFileResult(vocabulary, filename);
        }

        private async Task<Vocabulary> CreateVocabularyAsync(VocabularyId vocabularyId)
        {
            var vocabularyFactory = _vocabularyFactoryById[vocabularyId];
            return await vocabularyFactory.CreateVocabularyAsync();
        }

        private Vocabulary CreateVocabularyFromJsonFile(string filename)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(assemblyPath, filename);
            var str = File.ReadAllText(filePath, Encoding.UTF8);
            var vocabulary = JsonConvert.DeserializeObject<Vocabulary>(str);
            ValidateVocabulary(vocabulary);
            return vocabulary;
        }

        private void ValidateVocabulary(Vocabulary vocabulary)
        {
            var externalSystemMappingFields = vocabulary?.ExternalSystemsMapping?.SelectMany(mapping => mapping.Mappings);
            if (externalSystemMappingFields == null) return;

            foreach (var externalSystemMappingField in externalSystemMappingFields)
            {
                // Check if there exists duplicate synonyms.
                if (externalSystemMappingField.Values.First().Value is string)
                {
                    if (externalSystemMappingField.Values.Select(m => m.Value.ToString()?.ToLower()).HasDuplicates())
                    {
                        throw new Exception($"Duplicate mappings exist for field \"{vocabulary.Name}\"");
                    }
                }
                else
                {
                    if (externalSystemMappingField.Values.Select(m => m.Value).HasDuplicates())
                    {
                        throw new Exception($"Duplicate mappings exist for field \"{vocabulary.Name}\"");
                    }
                }
            }
        }

        private (string Filename, byte[] Bytes) CreateVocabularyFileResult(Vocabulary vocabulary, string fileName)
        {
            var bytes = SerializeToJsonArray(vocabulary,
                new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore}, Formatting.Indented);
            return (Filename: fileName, Bytes: bytes);
        }

        private byte[] SerializeToJsonArray(object value, JsonSerializerSettings jsonSerializerSettings,
            Formatting formatting)
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