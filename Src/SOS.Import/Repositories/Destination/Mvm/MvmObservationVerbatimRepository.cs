﻿using Microsoft.Extensions.Logging;
using SOS.Import.MongoDb.Interfaces;
using SOS.Lib.Models.Verbatim.Mvm;

namespace SOS.Import.Repositories.Destination.Mvm
{
    public class MvmObservationVerbatimRepository : VerbatimRepository<MvmObservationVerbatim, string>, Interfaces.IMvmObservationVerbatimRepository
    {
        public MvmObservationVerbatimRepository(
            IImportClient importClient,
            ILogger<MvmObservationVerbatimRepository> logger) : base(importClient, logger)
        {
        }
    }
}