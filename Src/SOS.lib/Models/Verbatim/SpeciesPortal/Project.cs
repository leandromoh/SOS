﻿using System;
using System.Collections.Generic;

namespace SOS.Lib.Models.Verbatim.SpeciesPortal
{
    /// <summary>
    /// Project class
    /// </summary>
    public class Project
    {
        /// <summary>
        /// Project category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Project description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Project end date
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Id of project
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// True if project is public
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Name of project
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Project owner
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Project start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Survey method used
        /// </summary>
        public string SurveyMethod { get; set; }

        /// <summary>
        /// Survey method url 
        /// </summary>
        public string SurveyMethodUrl { get; set; }

        /// <summary>
        /// Project parameters
        /// </summary>
        public List<ProjectParameter> ProjectParameters { get; set; }
    }
}