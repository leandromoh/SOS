﻿using System;
using MongoDB.Bson;
using SOS.Lib.Models.Interfaces;

namespace SOS.Lib.Models.Shared
{
    /// <summary>
    /// API Usage statistics.
    /// </summary>
    public class ApiUsageStatistics : IEntity<ObjectId>
    {
        /// <summary>
        /// API management user account id
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Average duration
        /// </summary>
        public long AverageDuration { get; set; }

        /// <summary>
        /// Issue date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// End point
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Number of failed calls
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// Method, GET, POST PUT etc
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///     Object id
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Number of request
        /// </summary>
        public long RequestCount { get; set; }

        /// <summary>
        /// User making the request
        /// </summary>
        public string UserId { get; set; }
    }
}