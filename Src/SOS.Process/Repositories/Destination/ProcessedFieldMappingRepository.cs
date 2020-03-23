﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;
using SOS.Process.Database.Interfaces;
using SOS.Process.Repositories.Destination.Interfaces;

namespace SOS.Process.Repositories.Destination
{
    /// <summary>
    /// Repository for retrieving field mappings.
    /// </summary>
    public class ProcessedFieldMappingRepository : ProcessBaseRepository<FieldMapping, FieldMappingFieldId>, IProcessedFieldMappingRepository
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ProcessedFieldMappingRepository(
            IProcessClient client, 
            ILogger<ProcessedFieldMappingRepository> logger) 
            : base(client, false, logger)
        {
           
        }

        /// <inheritdoc />
        public async Task<IEnumerable<FieldMapping>> GetFieldMappingsAsync()
        {
            try
            {
                const int batchSize = 200000;
                var skip = 0;
                var fieldMappingsChunk = (await GetChunkAsync(skip, batchSize)).ToArray();
                var fieldMappings = new List<FieldMapping>();

                while (fieldMappingsChunk?.Any() ?? false)
                {
                    fieldMappings.AddRange(fieldMappingsChunk);
                    skip += fieldMappingsChunk.Count();
                    fieldMappingsChunk = (await GetChunkAsync(skip, batchSize)).ToArray();
                }

                return fieldMappings;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to get chunk of field mappings");
                return null;
            }
        }

        private async Task<IEnumerable<FieldMapping>> GetChunkAsync(int skip, int take)
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