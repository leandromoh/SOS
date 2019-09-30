﻿
namespace SOS.Search.Service.Models.Interfaces
{
    /// <summary>
    /// IEntity interface
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface IEntity<TKey>
    {
        /// <summary>
        /// Id
        /// </summary>
        TKey Id { get; set; }
    }

    /// <summary>
    /// IEntity interface
    /// </summary>
    public interface IEntity : IEntity<string>
    {

    }
}