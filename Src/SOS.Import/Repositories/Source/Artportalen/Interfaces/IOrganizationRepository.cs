﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Import.Entities.Artportalen;

namespace SOS.Import.Repositories.Source.Artportalen.Interfaces
{
    /// <summary>
    ///     Organization repository interface.
    /// </summary>
    public interface IOrganizationRepository
    {
        /// <summary>
        ///     Get all organizations.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<OrganizationEntity>> GetAsync();
    }
}