﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Import.Entities;

namespace SOS.Import.Repositories.Source.Artportalen.Interfaces
{
    /// <summary>
    /// Site repository interface
    /// </summary>
    public interface ISiteRepository
    {
        /// <summary>
        /// Get all sites 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SiteEntity>> GetAsync();
    }
}