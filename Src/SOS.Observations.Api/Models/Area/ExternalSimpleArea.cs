﻿namespace SOS.Observations.Api.Models.Area
{
    public class ExternalSimpleArea
    {
        /// <summary>
        ///     Type of area
        /// </summary>
        public string AreaType { get; set; }

        /// <summary>
        /// Feature id
        /// </summary>
        public string Feature { get; set; }

        /// <summary>
        ///     Area Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     Name of area
        /// </summary>
        public string Name { get; set; }
    }
}