﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SOS.Import.Services.Interfaces;
using SOS.Lib.Configuration.Import;

namespace SOS.Import.Services
{
    /// <summary>
    ///     Artportalen data service
    /// </summary>
    public class ObservationDatabaseDataService : IObservationDatabaseDataService
    {
        /// <summary>
        /// Create new db connection
        /// </summary>
        /// <param name="live"></param>
        /// <returns></returns>
        private IDbConnection Connection => new SqlConnection(Configuration.ConnectionString);

        /// <summary>
        ///     Constructor
        /// </summary>
        public ObservationDatabaseDataService(ObservationDatabaseConfiguration observationDatabaseConfiguration)
        {
            Configuration = observationDatabaseConfiguration ??
                            throw new ArgumentNullException(nameof(observationDatabaseConfiguration));
        }

        /// <inheritdoc />
        public ObservationDatabaseConfiguration Configuration { get; }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> QueryAsync<T>(string query, dynamic parameters = null)
        {
            using var conn = Connection;
            conn.Open();
            var transaction = conn.BeginTransaction(IsolationLevel.ReadUncommitted);

            IEnumerable<T> result = null;
            try
            {
                result = (await conn.QueryAsync<T>(
                    new CommandDefinition(
                        query,
                        parameters,
                        transaction,
                        5 * 60, // 5 minutes
                        CommandType.Text,
                        CommandFlags.NoCache
                    )
                )).ToArray();

                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
            }

            return result;
        }
    }
}