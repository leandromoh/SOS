﻿using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SOS.Lib.Models.Search
{
    /// <summary>
    ///     Base filter class
    /// </summary>
    public class FilterBase
    {
        public enum SightingTypeFilter
        {
            DoNotShowMerged,
            ShowOnlyMerged,
            ShowBoth,
            DoNotShowSightingsInMerged
        }
        /// <summary>
        /// OverlappingStartDateAndEndDate, Start or EndDate of the observation must be within the specified interval    
        /// BetweenStartDateAndEndDate, Start and EndDate of the observation must be within the specified interval    
        /// OnlyStartDate, Only StartDate of the observation must be within the specified interval            
        /// OnlyEndDate, Only EndDate of the observation must be within the specified interval    
        /// </summary>
        public enum DateRangeFilterType
        {
            /// <summary>
            /// Start or EndDate of the observation must be within the specified interval
            /// </summary>
            OverlappingStartDateAndEndDate,
            /// <summary>
            /// Start and EndDate of the observation must be within the specified interval
            /// </summary>
            BetweenStartDateAndEndDate,
            /// <summary>
            /// Only StartDate of the observation must be within the specified interval
            /// </summary>
            OnlyStartDate,
            /// <summary>
            /// Only EndDate of the observation must be within the specified interval
            /// </summary>
            OnlyEndDate
        }

        /// <summary>
        /// Pre defined time ranges
        /// </summary>
        public enum TimeRange
        {
            /// <summary>
            /// 04:00-09:00
            /// </summary>
            Morning,
            /// <summary>
            /// 09:00-13:00
            /// </summary>
            Forenoon,
            /// <summary>
            /// 13:00-18:00
            /// </summary>
            Afternoon,
            /// <summary>
            /// 18:00-23:00
            /// </summary>
            Evening,
            /// <summary>
            /// 23:00-04:00
            /// </summary>
            Night
        }

        /// <summary>
        /// Geographical areas to filter by
        /// </summary>
        public IEnumerable<AreaFilter> Areas { get; set; }

        /// <summary>
        /// Bird validation area id's
        /// </summary>
        public ICollection<string> BirdValidationAreaIds { get; set; }

        /// <summary>
        /// County id's
        /// </summary>
        public ICollection<string> CountyIds { get; set; }

        /// <summary>
        ///     Only get data from these providers
        /// </summary>
        public IEnumerable<int> DataProviderIds { get; set; }

        /// <summary>
        ///     Observation end date specified in the ISO 8601 standard.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        ///     Field mapping translation culture code.
        ///     Available values.
        ///     sv-SE (Swedish)
        ///     en-GB (English)
        /// </summary>
        public string FieldTranslationCultureCode { get; set; }

        /// <summary>
        ///     Geometry filter 
        /// </summary>
        public GeometryFilter GeometryFilter { get; set; }

        /// <summary>
        ///     Gender to match. Queryable values are available in Field Mappings.
        /// </summary>
        public IEnumerable<int> GenderIds { get; set; }

        /// <summary>
        ///     Decides whether to search for the exact taxa or
        ///     for the hierarchical underlying taxa.
        /// </summary>
        public bool IncludeUnderlyingTaxa { get; set; }

        /// <summary>
        ///     Decides whether to search for the exact taxa or
        ///     for the hierarchical underlying taxa.
        /// </summary>

        /// <summary>
        ///     Municipalities to match. Queryable values are available in Field Mappings.
        /// </summary>
        public ICollection<string> MunicipalityIds { get; set; }

        /// <summary>
        ///     True to return only validated sightings.
        /// </summary>
        public bool? OnlyValidated { get; set; }

        /// <summary>
        ///     Parish to match. Queryable values are available in Field Mappings.
        /// </summary>
        public ICollection<string> ParishIds { get; set; }

        /// <summary>
        ///     True to return only positive sightings, false to return negative sightings, null to return both positive and
        ///     negative sightings.
        ///     An negative observation is an observation that was expected to be found but wasn't.
        /// </summary>
        public bool? PositiveSightings { get; set; }

        /// <summary>
        ///     Provinces to match. Queryable values are available in Field Mappings.
        /// </summary>
        public ICollection<string> ProvinceIds { get; set; }

        /// <summary>
        ///     Redlist categories to match. Queryable values are available in Field Mappings.
        /// </summary>
        public IEnumerable<string> RedListCategories { get; set; }

        /// <summary>
        ///     Which type of date filtering that should be used
        /// </summary>
        public DateRangeFilterType DateFilterType { get; set; } = DateRangeFilterType.OverlappingStartDateAndEndDate;

        public SightingTypeFilter TypeFilter { get; set; } = SightingTypeFilter.DoNotShowMerged;

        /// <summary>
        ///     Observation start date specified in the ISO 8601 standard.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        ///     Taxa to match. Queryable values are available in Field Mappings.
        /// </summary>
        public IEnumerable<int> TaxonIds { get; set; }

        /// <summary>
        /// Pre defined time ranges
        /// </summary>
        public IEnumerable<TimeRange> TimeRanges { get; set; }

        public FilterBase Clone()
        {
            var searchFilter = (FilterBase) MemberwiseClone();
            return searchFilter;
        }

        /// <summary>
        /// Convert filter to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}