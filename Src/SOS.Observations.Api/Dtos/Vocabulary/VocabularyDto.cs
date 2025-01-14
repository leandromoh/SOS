﻿using System.Collections.Generic;

namespace SOS.Observations.Api.Dtos.Vocabulary
{
    public class VocabularyDto
    {
        public int Id { get; set; }
        public VocabularyIdDto EnumId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Localized { get; set; }
        public IEnumerable<VocabularyValueInfoDto> Values { get; set; }
        public IEnumerable<ExternalSystemMappingDto> ExternalSystemsMapping { get; set; }
    }
}