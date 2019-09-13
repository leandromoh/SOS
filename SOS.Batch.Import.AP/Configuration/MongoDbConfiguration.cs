﻿namespace SOS.Batch.Import.AP.Configuration
{
    /// <summary>
    /// Cosmos configuration properties
    /// </summary>
    public class MongoDbConfiguration
    {
        /// <summary>
        /// Host
        /// </summary>
        public MongoDbServer[] Hosts { get; set; }

        /// <summary>
        /// Name of replica set
        /// </summary>
        public string ReplicaSetName { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Name of data base
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// True if ssl is used
        /// </summary>
        public bool UseSsl{ get; set; }

        /// <summary>
        /// How many items to add in a time
        /// </summary>
        public int AddBatchSize { get; set; }
    }
}