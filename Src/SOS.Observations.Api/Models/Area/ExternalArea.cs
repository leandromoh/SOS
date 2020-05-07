﻿using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;

namespace SOS.Observations.Api.Models.Area
{
    public class ExternalArea
    {
        /// <summary>
        /// Type of area
        /// </summary>
        public string AreaType { get; set; }

        /// <summary>
        /// Area geometry
        /// </summary>
        public GeometryGeoJson Geometry { get; set; }

        /// <summary>
        /// Area Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of area
        /// </summary>
        public string Name { get; set; }

        

    }
}