﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SOS.Batch.Import.AP.Services.Interfaces;

namespace SOS.Batch.Import.AP.Services
{
    /// <summary>
    /// Species portal data service
    /// </summary>
    public class SpeciesPortalDataService : ISpeciesPortalDataService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public SpeciesPortalDataService(IConfiguration config)
        {
            _connectionString = config?.GetConnectionString("SpeciesPortal") ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Create new db connection
        /// </summary>
        private IDbConnection Connection => new SqlConnection(_connectionString);

        /// <inheritdoc />
        public async Task<IEnumerable<T>> QueryAsync<T>(string query)
        {
            using (var conn = Connection)
            {
                conn.Open();

                return (await conn.QueryAsync<T>(
                    new CommandDefinition(
                        query,
                        null,
                        null,
                        null,
                        CommandType.Text,
                        CommandFlags.NoCache
                    )
                )).ToArray();
            }
        }
    }
}
