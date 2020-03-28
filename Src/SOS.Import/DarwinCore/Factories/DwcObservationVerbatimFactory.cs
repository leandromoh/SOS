﻿using DwC_A;
using DwC_A.Meta;
using SOS.Lib.Models.Verbatim.DarwinCore;

namespace SOS.Import.DarwinCore
{
    public static class DwcObservationVerbatimFactory
    {
        public static DwcObservationVerbatim Create(IRow row, string filename, int? idIndex = null)
        {
            var verbatimRecord = new DwcObservationVerbatim {DwcArchiveFilename = filename};
            foreach (FieldType fieldType in row.FieldMetaData)
            {
                var val = row[fieldType.Index];
                DwcTermValueMapper.MapValueByTerm(verbatimRecord, fieldType.Term, val);
            }
            if (idIndex.HasValue)
            {
                verbatimRecord.RecordId = row[idIndex.Value];
            }

            return verbatimRecord;
        }
    }
}