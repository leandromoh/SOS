﻿using System.Collections;
using Newtonsoft.Json;

namespace SOS.Lib.Models.Shared
{
    public class GeoJsonGeometry
    {
        /// <summary>
        ///     Geometry coordinates
        /// </summary>
        public ArrayList Coordinates { get; set; }

        /// <summary>
        ///     Simple check to check if geometry looks ok
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsValid
        {
            get
            {
                switch (Type?.ToLower())
                {
                    case "point":
                        return Coordinates?.Count == 2 && !Coordinates[0].Equals(0) && !Coordinates[1].Equals(0);
                    case "polygon":
                        return Coordinates?.Count != 0;
                    case "holepolygon":
                        return Coordinates?.Count != 0;
                    case "multipolygon":
                        return Coordinates?.Count != 0;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        ///     Type of geometry (point or polygon)
        /// </summary>
        public string Type { get; set; }
    }
}