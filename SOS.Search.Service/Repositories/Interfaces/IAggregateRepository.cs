﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Search.Service.Models.Interfaces;

namespace SOS.Search.Service.Repositories.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IAggregateRepository<TEntity, in TKey> : IDisposable where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// Get entity
        /// </summary>
        /// <returns></returns>
        Task<TEntity> GetAsync(TKey id);

        /// <summary>
        /// Get all objects in repository
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> GetAllAsync();
    }
}