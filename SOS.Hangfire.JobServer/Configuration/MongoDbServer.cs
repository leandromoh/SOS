﻿namespace SOS.Hangfire.JobServer.Configuration
{
    /// <summary>
    /// Mongo db server properties
    /// </summary>
    public class MongoDbServer
    {
        /// <summary>
        /// Host
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; set; }
    }
}