﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SOS.Observations.Api.Dtos.Vocabulary
{
    public class VocabularyValueInfoDto
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public bool Localized { get; set; }

        public VocabularyValueInfoCategoryDto Category { get; set; }

        /// <summary>
        ///     Translations.
        /// </summary>
        /// <remarks>
        ///     Translations exists if the <see cref="Localized" /> property is set to true.
        /// </remarks>
        public ICollection<VocabularyValueTranslationDto> Translations { get; set; }

    }
}
