﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SOS.Import.Entities.Artportalen;
using SOS.Import.Repositories.Source.Artportalen.Interfaces;
using SOS.Import.Services.Interfaces;

namespace SOS.Import.Repositories.Source.Artportalen
{
    public class PersonRepository : BaseRepository<PersonRepository>, IPersonRepository
    {
        public PersonRepository(
            IArtportalenDataService artportalenDataService,
            ILogger<PersonRepository> logger) : base(artportalenDataService, logger)
        {
        }

        public async Task<IEnumerable<PersonEntity>> GetAsync()
        {
            try
            {
                const string query = @"
                SELECT 
	                p.Id,
	                u.Id as UserId,	                
	                p.FirstName,
	                p.LastName,
                    u.UserAlias AS Alias,
                    u.UserServiceUserId
                FROM 
	                [Person] p
                    INNER JOIN [User] u ON p.Id = u.PersonId";

                return await QueryAsync<PersonEntity>(query);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error getting persons");
                return null;
            }
        }
    }
}