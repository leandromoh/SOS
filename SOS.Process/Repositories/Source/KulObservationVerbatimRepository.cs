﻿using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using SOS.Lib.Models.Verbatim.Kul;
using SOS.Process.Database.Interfaces;

namespace SOS.Process.Repositories.Source
{
    public class KulObservationVerbatimRepository : VerbatimBaseRepository<KulObservationVerbatim, string>, Interfaces.IKulObservationVerbatimRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public KulObservationVerbatimRepository(
            IVerbatimClient client,
            ILogger<KulObservationVerbatimRepository> logger) : base(client, logger)
        {

        }
    }
}