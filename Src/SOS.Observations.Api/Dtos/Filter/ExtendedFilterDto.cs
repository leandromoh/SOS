﻿using System;
using System.Collections.Generic;

namespace SOS.Observations.Api.Dtos.Filter
{
    public class ExtendedFilterDto
    {
        public enum SightingTypeFilterDto
        {
            DoNotShowMerged,
            ShowOnlyMerged,
            ShowBoth,
            DoNotShowSightingsInMerged
        }

        public enum SightingUnspontaneousFilterDto
        {
            NoFilter,
            NotUnspontaneous,
            Unspontaneous
        }
        public enum SightingNotPresentFilterDto
        {
            DontIncludeNotPresent,
            OnlyNotPresent,
            IncludeNotPresent
        }

        public enum MonthsFilterComparisonDto
        {
            StartDate,
            EndDate,
            BothStartDateAndEndDate
        }

        /// <summary>
        /// Reported by Artportalen user id.
        /// </summary>
        public int? ReportedByUserId { get; set; }
        
        /// <summary>
        /// Observed by Artportalen user id.
        /// </summary>
        public int? ObservedByUserId { get; set; }

        /// <summary>
        /// Reported by user service user id.
        /// </summary>
        public int? ReportedByUserServiceUserId { get; set; }
        
        /// <summary>
        /// Observed by user service user id.
        /// </summary>
        public int? ObservedByUserServiceUserId { get; set; }

        /// <summary>
        /// Id of sex to match
        /// </summary>
        public IEnumerable<int> SexIds { get; set; }

        /// <summary>
        /// Only include hits with media associated
        /// </summary>
        public bool OnlyWithMedia { get; set; }
        /// <summary>
        /// Only include hits with notes attached to them
        /// </summary>
        public bool OnlyWithNotes { get; set; }

        public bool OnlyWithNotesOfInterest { get; set; }
        /// <summary>
        /// Only include hits that have user comments on them
        /// </summary>
        public bool OnlyWithUserComments { get; set; } = false;

        public bool OnlyWithBarcode { get; set; }

        public DateTime? ReportedDateFrom { get; set; }
        public DateTime? ReportedDateTo { get; set; }
        public SightingTypeFilterDto TypeFilter { get; set; } = SightingTypeFilterDto.DoNotShowMerged;
        
        public bool UsePeriodForAllYears { get; set; }
        public IEnumerable<int> Months { get; set; }
        public MonthsFilterComparisonDto MonthsComparison { get; set; } = MonthsFilterComparisonDto.StartDate;
        public IEnumerable<int> DiscoveryMethodIds { get; set; }

        public IEnumerable<int> LifeStageIds { get; set; }

        public IEnumerable<int> ActivityIds { get; set; }

        public bool HasTriggerdValidationRule { get; set; }
        public bool HasTriggerdValidationRuleWithWarning { get; set; }

        public int? Length { get; set; }
        public string LengthOperator { get; set; }
        public int? Weight { get; set; }
        public string WeightOperator { get; set; }

        public int? Quantity { get; set; }
        public string QuantityOperator { get; set; }

        public IEnumerable<int> ValidationStatusIds { get; set; }
        public IEnumerable<int> ExcludeValidationStatusIds { get; set; }

        public SightingUnspontaneousFilterDto UnspontaneousFilter { get; set; }

        public string SpeciesCollectionLabel { get; set; }

        public string PublicCollection { get; set; }

        public string PrivateCollection { get; set; }

        public int? SubstrateSpeciesId { get; set; }
        public int? SubstrateId { get; set; }

        public int? BiotopeId { get; set; }

        public SightingNotPresentFilterDto NotPresentFilter { get; set; }

        public bool OnlySecondHandInformation { get; set; }

        public IEnumerable<int> PublishTypeIdsFilter { get; set; }

        public IEnumerable<int> RegionalSightingStateIdsFilter { get; set; }

        public IEnumerable<int> SiteIds { get; set; }

        public IEnumerable<int> SpeciesFactsIds { get; set; }
        public string InstitutionId { get; set; }

        public IEnumerable<int> DatasourceIds { get; set; }

        public string LocationNameFilter { get; set; }
    }
}