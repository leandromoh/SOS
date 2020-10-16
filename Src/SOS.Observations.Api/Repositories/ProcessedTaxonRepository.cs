﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SOS.Lib.Database.Interfaces;
using SOS.Lib.Models.Processed.Observation;
using SOS.Observations.Api.Repositories.Interfaces;

namespace SOS.Observations.Api.Repositories
{
    /// <summary>
    /// </summary>
    public class ProcessedTaxonRepository : ProcessBaseRepository<Taxon, int>, IProcessedTaxonRepository
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ProcessedTaxonRepository(
            IProcessClient client,
            ILogger<ProcessBaseRepository<Taxon, int>> logger) : base(client, false, logger)
        {
        }

        /// <summary>
        ///     Get chunk of taxa
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BasicTaxon>> GetBasicTaxonChunkAsync(int skip, int take)
        {
            var res = await MongoCollection
                .Find(x => true)
                .Project(m => new BasicTaxon
                {
                    DyntaxaTaxonId = m.DyntaxaTaxonId,
                    Id = m.Id,
                    ParentDyntaxaTaxonId = m.ParentDyntaxaTaxonId,
                    SecondaryParentDyntaxaTaxonIds = m.SecondaryParentDyntaxaTaxonIds,
                    ScientificName = m.ScientificName
                })
                .Skip(skip)
                .Limit(take)
                .ToListAsync();

            return res;
        }

        /// <summary>
        ///     Get chunk of taxa
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Taxon>> GetChunkAsync(int skip, int take)
        {
            var res = await MongoCollection
                .Find(x => true)
                .Skip(skip)
                .Limit(take)
                .ToListAsync();

            return res;
        }
    }
}