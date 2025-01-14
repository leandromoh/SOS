﻿using System;
using System.Collections.Generic;
using Nest;

namespace SOS.Lib.Models.Processed.Observation
{
    /// <summary>
    ///     Artportalen project information.
    /// </summary>
    public class Project : ProjectInfo
    {
        /// <summary>
        ///     Project parameters.
        /// </summary>
        [Nested]
        public IEnumerable<ProjectParameter> ProjectParameters { get; set; }
        public override string ToString()
        {
            string strProjectParameters = ProjectParameters == null ? null : string.Join(", ", ProjectParameters);
            if (string.IsNullOrEmpty(strProjectParameters))
                return $"{Name} [{Id}]";
            else
                return $"{Name} [{Id}], Params: {strProjectParameters}";
        }
    }
}