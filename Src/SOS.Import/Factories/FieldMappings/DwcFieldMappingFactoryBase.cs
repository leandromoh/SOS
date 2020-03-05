﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SOS.Import.Entities;
using SOS.Import.Extensions;
using SOS.Import.Factories.FieldMappings.Interfaces;
using SOS.Lib.Constants;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;

namespace SOS.Import.Factories.FieldMappings
{
    public abstract class DwcFieldMappingFactoryBase : IFieldMappingCreatorFactory
    {
        protected abstract FieldMappingFieldId FieldId { get; }
        protected abstract bool Localized { get; }
        protected abstract ICollection<FieldMappingValue> GetFieldMappingValues();
        public virtual async Task<FieldMapping> CreateFieldMappingAsync()
        {
            var fieldMappingValues = GetFieldMappingValues();

            FieldMapping fieldMapping = new FieldMapping
            {
                Id = FieldId,
                Name = FieldId.ToString(),
                Localized = Localized,
                Values = fieldMappingValues,
                ExternalSystemsMapping = GetExternalSystemMappings(fieldMappingValues)
            };

            return fieldMapping;
        }

        protected virtual List<ExternalSystemMapping> GetExternalSystemMappings(ICollection<FieldMappingValue> fieldMappingValues)
        {
            return new List<ExternalSystemMapping>()
            {
                GetDwcExternalSystemMapping(fieldMappingValues)
            };
        }

        protected virtual ExternalSystemMapping GetDwcExternalSystemMapping(ICollection<FieldMappingValue> fieldMappingValues)
        {
            ExternalSystemMapping dwcMapping = new ExternalSystemMapping
            {
                Id = ExternalSystemId.DarwinCore,
                Name = ExternalSystemId.DarwinCore.ToString(),
                Description = "The Darwin Core format (https://dwc.tdwg.org/terms/)",
                Mappings = new List<ExternalSystemMappingField>()
            };

            string term = FieldId.ToString().ToLowerFirstChar();
            ExternalSystemMappingField mappingField = new ExternalSystemMappingField
            {
                Key = term,
                Description = $"The {term} term (http://rs.tdwg.org/dwc/terms/{term})",
                Values = new List<ExternalSystemMappingValue>()
            };

            foreach (var fieldMappingValue in fieldMappingValues)
            {
                mappingField.Values.Add(new ExternalSystemMappingValue
                {
                    Value = fieldMappingValue.Name,
                    SosId = fieldMappingValue.Id
                });

                var stringVariations = GetStringVariations(fieldMappingValue.Name);
                foreach (var stringVariation in stringVariations)
                {
                    mappingField.Values.Add(new ExternalSystemMappingValue
                    {
                        Value = stringVariation,
                        SosId = fieldMappingValue.Id
                    });
                }
            }

            dwcMapping.Mappings.Add(mappingField);
            return dwcMapping;
        }

        private List<string> GetStringVariations(string str)
        {
            List<string> stringVariations = new List<string>();
            string[] wordParts = SplitCamelCaseString(str);
            if (wordParts.Length > 1)
            {
                string strVariation = string.Join(" ", wordParts);
                if (strVariation != str)
                {
                    stringVariations.Add(strVariation);
                }
            }

            return stringVariations;
        }

        private string[] SplitCamelCaseString(string str)
        {
            return Regex.Matches(str, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();
        }
    }
}