﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SOS.Import.Entities;
using SOS.Import.Repositories.Source.SpeciesPortal.Interfaces;
using SOS.Lib.Constants;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;

namespace SOS.Import.Factories.FieldMappings
{

    /// <summary>
    /// Class for creating Activity field mapping.
    /// </summary>
    public class ActivityFieldMappingFactory : ArtportalenFieldMappingFactoryBase
    {
        private readonly IMetadataRepository _artportalenMetadataRepository;
        private readonly ILogger<ActivityFieldMappingFactory> _logger;
        protected override FieldMappingFieldId FieldId => FieldMappingFieldId.Activity;
        protected override bool Localized => true;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="artportalenMetadataRepository"></param>
        /// <param name="logger"></param>
        public ActivityFieldMappingFactory(
            IMetadataRepository artportalenMetadataRepository,
            ILogger<ActivityFieldMappingFactory> logger)
        {
            _artportalenMetadataRepository = artportalenMetadataRepository ?? throw new ArgumentNullException(nameof(artportalenMetadataRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<ICollection<FieldMappingValue>> GetFieldMappingValues()
        {
            IEnumerable<MetadataWithCategoryEntity> activities = await _artportalenMetadataRepository.GetActivitiesAsync();
            var fieldMappingValues = ConvertToFieldMappingValuesWithCategory(activities.ToArray());
            return fieldMappingValues;
        }
    }
}