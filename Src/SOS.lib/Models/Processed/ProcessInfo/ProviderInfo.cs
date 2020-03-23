﻿using System;
using System.Collections.Generic;
using SOS.Lib.Enums;

namespace SOS.Lib.Models.Processed.ProcessInfo
{
    /// <summary>
    /// Information about verbatim
    /// </summary>
    public class ProviderInfo 
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="provider"></param>
        public ProviderInfo(DataSet provider)
        {
            Provider = provider;
        }

        /// <summary>
        /// Number of items harvested
        /// </summary>
        public int? HarvestCount { get; set; }

        /// <summary>
        /// Harvest end date and time
        /// </summary>
        public DateTime? HarvestEnd { get; set; }

        /// <summary>
        /// Harvest start date and time
        /// </summary>
        public DateTime? HarvestStart { get; set; }

        /// <summary>
        /// Status of harvest
        /// </summary>
        public RunStatus? HarvestStatus { get; set; }

        /// <summary>
        /// Number of items processed
        /// </summary>
        public int? ProcessCount { get; set; }

        /// <summary>
        /// Process end date and time
        /// </summary>
        public DateTime? ProcessEnd { get; set; }

        /// <summary>
        /// Process start date and time
        /// </summary>
        public DateTime ProcessStart { get; set; }

        /// <summary>
        /// Status of processing
        /// </summary>
        public RunStatus? ProcessStatus { get; set; }

        /// <summary>
        /// Id of data provider
        /// </summary>
        public DataSet Provider { get; private set; }

        /// <summary>
        /// Provider information about meta data
        /// </summary>
        public IEnumerable<ProviderInfo> MetadataInfo { get; set; }
    }
}
