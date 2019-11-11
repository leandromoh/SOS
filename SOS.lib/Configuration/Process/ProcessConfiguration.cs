﻿using SOS.Lib.Configuration.Shared;

namespace SOS.Lib.Configuration.Process
{
    /// <summary>
    /// Root config
    /// </summary>
    public class ProcessConfiguration
    {
        /// <summary>
        /// Application settings
        /// </summary>
        public AppSettings AppSettings { get; set; }
        /// <summary>
        /// Host
        /// </summary>
        public MongoDbConfiguration VerbatimDbConfiguration { get; set; }

        /// <summary>
        /// Host
        /// </summary>
        public MongoDbConfiguration ProcessedDbConfiguration { get; set; }
    }
}