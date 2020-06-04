﻿namespace SOS.Import.Entities.Artportalen
{
    /// <summary>
    ///     Site object
    /// </summary>
    public class SiteEntity
    {
        /// <summary>
        ///     Accuracy in meters
        /// </summary>
        public int Accuracy { get; set; }

        /// <summary>
        ///     Id of county
        /// </summary>
        public int? CountyId { get; set; }

        /// <summary>
        ///     Id of county
        /// </summary>
        public string CountyName { get; set; }

        /// <summary>
        ///     Country part id
        /// </summary>
        public int? CountryPartId { get; set; }

        /// <summary>
        ///     Name of country part
        /// </summary>
        public string CountryPartName { get; set; }

        /// <summary>
        ///     Name of county
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     Id of municipality
        /// </summary>
        public int? MunicipalityId { get; set; }

        /// <summary>
        ///     Name of municipality
        /// </summary>
        public string MunicipalityName { get; set; }

        /// <summary>
        ///     Id of Parish
        /// </summary>
        public int? ParishId { get; set; }

        /// <summary>
        ///     Name of Parish
        /// </summary>
        public string ParishName { get; set; }

        /// <summary>
        ///     Presentation name with Parish
        /// </summary>
        public string PresentationNameParishRegion { get; set; }

        /// <summary>
        ///     Id of province
        /// </summary>
        public int? ProvinceId { get; set; }

        /// <summary>
        ///     Name of province
        /// </summary>
        public string ProvinceName { get; set; }

        /// <summary>
        ///     Name of site
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     X coordinate of site
        /// </summary>
        public int XCoord { get; set; }

        /// <summary>
        ///     Y coordinate of site
        /// </summary>
        public int YCoord { get; set; }

        /// <summary>
        ///     Parent side id, exposed only to the internal api
        /// </summary>
        public int? ParentSiteId { get; set; }
    }
}