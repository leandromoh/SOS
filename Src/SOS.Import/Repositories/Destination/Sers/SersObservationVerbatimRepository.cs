﻿using Microsoft.Extensions.Logging;
using SOS.Import.MongoDb.Interfaces;
using SOS.Lib.Models.Verbatim.Sers;

namespace SOS.Import.Repositories.Destination.Sers
{
    public class SersObservationVerbatimRepository : VerbatimRepository<SersObservationVerbatim, string>, Interfaces.ISersObservationVerbatimRepository
    {
        public SersObservationVerbatimRepository(
            IImportClient importClient,
            ILogger<SersObservationVerbatimRepository> logger) : base(importClient, logger)
        {
        }
    }
}