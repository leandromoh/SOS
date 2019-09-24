﻿namespace SOS.Search.Service.Configuration
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
        /// Port
        /// </summary>
        public int Port { get; set; }

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
    }
}