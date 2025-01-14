﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Import.Entities.Artportalen;

namespace SOS.Import.Repositories.Source.Artportalen.Interfaces
{
    /// <summary>
    ///     Metadata repository interface
    /// </summary>
    public interface IMetadataRepository
    {
        /// <summary>
        ///     Get all activities
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataWithCategoryEntity>> GetActivitiesAsync();

        /// <summary>
        ///     Get all bioptopes
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetBiotopesAsync();

        /// <summary>
        ///     Get all genders
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetGendersAsync();

        /// <summary>
        ///     Get all organizations
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetOrganizationsAsync();

        /// <summary>
        ///     Get all stages
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetStagesAsync();

        /// <summary>
        ///     Get all substrates
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetSubstratesAsync();

        /// <summary>
        ///     Get all units
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetUnitsAsync();

        /// <summary>
        ///     Get all validation status items
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetValidationStatusAsync();

        /// <summary>
        ///     Gets all area types
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetAreaTypesAsync();
        
        /// <summary>
        ///     Gets all discovery methods
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetDiscoveryMethodsAsync();

        /// <summary>
        ///     Gets all determination methods
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MetadataEntity>> GetDeterminationMethodsAsync();

        /// <summary>
        /// Get date backup was taken
        /// </summary>
        /// <returns></returns>
        Task<DateTime?> GetLastBackupDateAsync();
    }
}