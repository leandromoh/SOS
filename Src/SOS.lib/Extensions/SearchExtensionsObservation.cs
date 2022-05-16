﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nest;
using SOS.Lib.Enums;
using SOS.Lib.Enums.Artportalen;
using SOS.Lib.Extensions;
using SOS.Lib.Models.Search;
using static SOS.Lib.Extensions.SearchExtensionsGeneric;

namespace SOS.Lib
{
    /// <summary>
    /// Observation specific search related extensions
    /// </summary>
    public static class SearchExtensionsObservation
    {
        /// <summary>
        /// Add filter to limit response to only show observations user is allowed to see
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void AddAuthorizationFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, ExtendedAuthorizationFilter filter)
        {
            if (filter.ReportedByMe)
            {
                query.TryAddTermCriteria("artportalenInternal.reportedByUserServiceUserId",
                    filter.UserId);
            }

            if (filter.ObservedByMe)
            {
                query.TryAddNestedTermAndCriteria("artportalenInternal.occurrenceRecordedByInternal", new Dictionary<string, object> { 
                    { "userServiceUserId", filter.UserId },
                    { "viewAccess", true }
                });
            }
            
            if (!filter.ProtectedObservations)
            {
                return;
            }

            var authorizeQuerys = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            if (filter.ExtendedAreas?.Any() ?? false)
            {
                foreach (var extendedAuthorization in filter.ExtendedAreas)
                {
                    var protectedQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();
                    protectedQuery.TryAddTermCriteria("sensitive", true);
                    protectedQuery.TryAddNumericRangeCriteria("occurrence.sensitivityCategory", extendedAuthorization.MaxProtectionLevel, SearchExtensionsGeneric.RangeTypes.LessThanOrEquals);
                    protectedQuery.TryAddTermsCriteria("taxon.id", extendedAuthorization.TaxonIds);
                    TryAddGeographicalAreaFilter(protectedQuery, extendedAuthorization.GeographicAreas);

                    authorizeQuerys.Add(q => q
                        .Bool(b => b
                            .Filter(protectedQuery)
                        )
                    );
                }
            }

            if (filter.UserId != 0)
            {
                // Add autorization to a users 'own' observations 
                var observedByMeQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();
                observedByMeQuery.TryAddTermCriteria("artportalenInternal.reportedByUserServiceUserId",
                    filter.UserId);

                authorizeQuerys.Add(q => q
                    .Bool(b => b
                        .Filter(observedByMeQuery)
                    )
                );

                var reportedByMeQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();
                reportedByMeQuery.TryAddNestedTermAndCriteria("artportalenInternal.occurrenceRecordedByInternal", new Dictionary<string, object> {
                    { "userServiceUserId", filter.UserId },
                    { "viewAccess", true }
                });

                authorizeQuerys.Add(q => q
                    .Bool(b => b
                        .Filter(reportedByMeQuery)
                    )
                );
            }

            if (authorizeQuerys.Any())
            {
                query.Add(q => q
                    .Bool(b => b
                        .Should(authorizeQuerys)
                    )
                );
                return;
            }

            // No extended authorization. Make sure only public data match
            query.TryAddNumericRangeCriteria("occurrence.sensitivityCategory", 2, SearchExtensionsGeneric.RangeTypes.LessThanOrEquals);
        }

        /// <summary>
        /// Add internal filters to query
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        private static void AddInternalFilters(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, SearchFilterBase filter)
        {
            var internalFilter = filter as SearchFilterInternal;

            query.TryAddTermCriteria("artportalenInternal.checkListId", internalFilter.CheckListId);
            query.TryAddTermsCriteria("artportalenInternal.datasourceId", internalFilter.DatasourceIds);
            query.TryAddTermCriteria("artportalenInternal.hasTriggeredVerificationRules", internalFilter.HasTriggeredVerificationRule, true);
            query.TryAddTermCriteria("artportalenInternal.hasAnyTriggeredVerificationRuleWithWarning", internalFilter.HasTriggeredVerificationRuleWithWarning, true);
            query.TryAddTermCriteria("artportalenInternal.hasUserComments", internalFilter.OnlyWithUserComments, true);

            switch (internalFilter.UnspontaneousFilter)
            {
                case SightingUnspontaneousFilter.NotUnspontaneous:
                    query.TryAddTermCriteria("occurrence.isNaturalOccurrence", true);
                    break;
                case SightingUnspontaneousFilter.Unspontaneous:
                    query.TryAddTermCriteria("occurrence.isNaturalOccurrence", false);
                    break;
            }

            query.TryAddTermCriteria("artportalenInternal.noteOfInterest", internalFilter.OnlyWithNotesOfInterest, true);
            query.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal", "id", internalFilter.ObservedByUserId);
            query.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal", "userServiceUserId", internalFilter.ObservedByUserServiceUserId);

            //search by locationId, but include child-locations observations aswell
            var siteTerms = internalFilter?.SiteIds?.Select(s => $"urn:lsid:artportalen.se:site:{s}");
            if (siteTerms?.Any() ?? false)
            {

                query.Add(q => q
                    .Bool(p => p
                        .Should(s => s
                            .Terms(t => t
                                .Field("location.locationId")
                                .Terms(siteTerms)),
                            s => s
                            .Terms(t => t
                                .Field("artportalenInternal.parentLocationId")
                                .Terms(internalFilter.SiteIds))
                             )
                        )
                    );
            }

            query.TryAddTermsCriteria("artportalenInternal.regionalSightingStateId", internalFilter.RegionalSightingStateIdsFilter);
            query.TryAddTermCriteria("artportalenInternal.reportedByUserId", internalFilter.ReportedByUserId);
            query.TryAddTermCriteria("artportalenInternal.reportedByUserServiceUserId", internalFilter.ReportedByUserServiceUserId);

            if (internalFilter.OnlySecondHandInformation)
            {
                query.TryAddTermCriteria("artportalenInternal.secondHandInformation", true);
            }

            if (internalFilter.OnlyWithBarcode)
            {
                query.AddMustExistsCriteria("artportalenInternal.sightingBarcodeURL");
            }

            query.TryAddTermsCriteria("artportalenInternal.sightingPublishTypeIds", internalFilter.PublishTypeIdsFilter);

            if (internalFilter.SpeciesFactsIds?.Any() ?? false)
            {
                foreach (var factsId in internalFilter.SpeciesFactsIds)
                {
                    query.TryAddTermCriteria("artportalenInternal.speciesFactsIds", factsId);
                }
            }

            query.TryAddTermsCriteria("artportalenInternal.triggeredObservationRuleFrequencyId", internalFilter.TriggeredObservationRuleFrequencyIds);
            query.TryAddTermsCriteria("artportalenInternal.triggeredObservationRuleReproductionId", internalFilter.TriggeredObservationRuleReproductionIds);
          
            query.TryAddTermsCriteria("event.discoveryMethod.id", internalFilter.DiscoveryMethodIds);

            query.TryAddTermsCriteria("identification.verificationStatus.id", internalFilter.VerificationStatusIds);
            query.TryAddTermCriteria("institutionId", internalFilter.InstitutionId);

            query.TryAddTermsCriteria("location.attributes.projectId", internalFilter.SiteProjectIds);
            query.TryAddWildcardCriteria("location.locality", internalFilter?.Location?.NameFilter);

            query.TryAddTermsCriteria("occurrence.activity.id", internalFilter.ActivityIds);
            
            if (internalFilter.OnlyWithMedia)
            {
                query.AddMustExistsCriteria("occurrence.associatedMedia");
                //    query.TryAddWildcardCriteria("occurrence.associatedMedia", "http*");
            }

            query.TryAddTermCriteria("occurrence.biotope.id", internalFilter.BiotopeId);

            switch (internalFilter.NotPresentFilter)
            {
                case SightingNotPresentFilter.DontIncludeNotPresent:
                    query.TryAddTermCriteria("occurrence.isNeverFoundObservation", false);
                    break;
                case SightingNotPresentFilter.OnlyNotPresent:
                    query.TryAddTermCriteria("occurrence.isNeverFoundObservation", true);
                    break;
            }

            if (internalFilter.Length.HasValue && !string.IsNullOrWhiteSpace(internalFilter.LengthOperator))
            {
                query.AddNumericFilterWithRelationalOperator("occurrence.length", internalFilter.Length.Value, internalFilter.LengthOperator);
            }

            query.TryAddTermsCriteria("occurrence.lifeStage.id", internalFilter.LifeStageIds);

            if (internalFilter.OnlyWithNotes)
            {
                query.AddMustExistsCriteria("occurrence.occurrenceRemarks");
                //  query.TryAddWildcardCriteria("occurrence.occurrenceRemarks", "?*");
            }

            if (internalFilter.Quantity.HasValue && !string.IsNullOrWhiteSpace(internalFilter.QuantityOperator))
            {
                query.AddNumericFilterWithRelationalOperator("occurrence.organismQuantityInt", internalFilter.Quantity.Value, internalFilter.QuantityOperator);
            }

            query.TryAddDateRangeCriteria("occurrence.reportedDate", internalFilter.ReportedDateFrom, SearchExtensionsGeneric.RangeTypes.GreaterThanOrEquals);
            query.TryAddDateRangeCriteria("occurrence.reportedDate", internalFilter.ReportedDateTo, SearchExtensionsGeneric.RangeTypes.LessThanOrEquals);

            query.TryAddTermCriteria("occurrence.substrate.id", internalFilter.SubstrateId);
            query.TryAddTermCriteria("occurrence.substrate.speciesId", internalFilter.SubstrateSpeciesId);
            
            if (internalFilter.Weight.HasValue && !string.IsNullOrWhiteSpace(internalFilter.WeightOperator))
            {
                query.AddNumericFilterWithRelationalOperator("occurrence.weight", internalFilter.Weight.Value, internalFilter.WeightOperator);
            }

            query.TryAddTermCriteria("privateCollection", internalFilter.PrivateCollection);
            query.TryAddTermCriteria("publicCollection", internalFilter.PublicCollection);

            query.TryAddTermCriteria("speciesCollectionLabel", internalFilter.SpeciesCollectionLabel);

            if (internalFilter.Months?.Any() ?? false)
            {
                switch (internalFilter.MonthsComparison)
                {
                    case DateFilterComparison.BothStartDateAndEndDate:
                        query.TryAddTermsCriteria("event.startMonth", internalFilter.Months);
                        query.TryAddTermsCriteria("event.endMonth", internalFilter.Months);
                        break;
                    case DateFilterComparison.EndDate:
                        query.TryAddTermsCriteria("event.endMonth", internalFilter.Months);
                        break;
                    default:
                        query.TryAddTermsCriteria("event.startMonth", internalFilter.Months);
                        break;
                }
            }

            if (internalFilter.Years?.Any() ?? false)
            {
                switch (internalFilter.YearsComparison)
                {
                    case DateFilterComparison.BothStartDateAndEndDate:
                        query.TryAddTermsCriteria("event.startYear", internalFilter.Years);
                        query.TryAddTermsCriteria("event.endYear", internalFilter.Years);
                        break;
                    case DateFilterComparison.EndDate:
                        query.TryAddTermsCriteria("event.endYear", internalFilter.Years);
                        break;
                    default:
                        query.TryAddTermsCriteria("event.startYear", internalFilter.Years);
                        break;
                }
            }

            if (internalFilter.Date != null && internalFilter.UsePeriodForAllYears && internalFilter.Date.StartDate.HasValue && internalFilter.Date.EndDate.HasValue)
            {
                var selector = "";

                if (filter.Date.DateFilterType == DateFilter.DateRangeFilterType.BetweenStartDateAndEndDate)
                {
                    selector = "((startMonth > fromMonth || (startMonth == fromMonth && startDay >= fromDay)) && (endMonth < toMonth || (endMonth == toMonth && endDay <= toDay)))";
                }
                else if (filter.Date.DateFilterType == DateFilter.DateRangeFilterType.OnlyStartDate)
                {
                    selector = "((startMonth > fromMonth || (startMonth == fromMonth && startDay >= fromDay)) && (startMonth < toMonth || (startMonth == toMonth && startDay <= toDay)))";
                }
                else if (filter.Date.DateFilterType == DateFilter.DateRangeFilterType.OnlyEndDate)
                {
                    selector = "((endMonth > fromMonth || (endMonth == fromMonth && endDay >= fromDay)) && (endMonth < toMonth || (endMonth == toMonth && endDay <= toDay)))";
                }
                else if (filter.Date.DateFilterType == DateFilter.DateRangeFilterType.OverlappingStartDateAndEndDate)
                {
                    selector = "((startMonth > fromMonth || (startMonth == fromMonth && startDay >= fromDay)) && (startMonth < toMonth || (startMonth == toMonth && startDay <= toDay))) || " +
                               "((endMonth > fromMonth || (endMonth == fromMonth && endDay >= fromDay)) && (endMonth < toMonth || (endMonth == toMonth && endDay <= toDay)))";
                }

                query.AddScript($@"
                            ZonedDateTime zStartDate = doc['event.startDate'].value;  
                            ZonedDateTime convertedStartDate = zStartDate.withZoneSameInstant(ZoneId.of('Europe/Stockholm'));
                            int startMonth = convertedStartDate.getMonthValue();
                            int startDay = convertedStartDate.getDayOfMonth();
                            
                            ZonedDateTime zEndDate = doc['event.endDate'].value;  
                            ZonedDateTime convertedEndDate = zEndDate.withZoneSameInstant(ZoneId.of('Europe/Stockholm'));
                            int endMonth = convertedEndDate.getMonthValue();
                            int endDay = convertedEndDate.getDayOfMonth();

                            int fromMonth = {internalFilter.Date.StartDate.Value.Month};
                            int fromDay = {internalFilter.Date.StartDate.Value.Day};
                            int toMonth = {internalFilter.Date.EndDate.Value.Month};
                            int toDay = {internalFilter.Date.EndDate.Value.Day};

                            if(
                               {selector}
                            )
                            {{ 
                                return true;
                            }} 
                            else 
                            {{
                                return false;
                            }}
                        ");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="excludeQuery"></param>
        /// <returns></returns>
        private static void AddInternalExcludeFilters(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> excludeQuery, SearchFilterBase filter)
        {
            var internalFilter = filter as SearchFilterInternal;

            excludeQuery.TryAddTermsCriteria("identification.verificationStatus.id", internalFilter.ExcludeVerificationStatusIds);
        }

        private static void AddSightingTypeFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, SearchFilterBase filter)
        {
            var sightingTypeQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            // Default DoNotShowMerged
            var sightingTypeSearchGroupFilter = new[] { // 1, 4, 16, 32, 128
                (int)SightingTypeSearchGroup.Ordinary,
                (int)SightingTypeSearchGroup.Aggregated,
                (int)SightingTypeSearchGroup.AssessmentChild,
                (int)SightingTypeSearchGroup.Replacement,
                (int)SightingTypeSearchGroup.OwnBreedingAssessment };

            if (filter.TypeFilter == SearchFilterBase.SightingTypeFilter.ShowBoth)
            {
                sightingTypeSearchGroupFilter = new[] { // 1, 2, 4, 16, 32, 128
                    (int)SightingTypeSearchGroup.Ordinary,
                    (int)SightingTypeSearchGroup.Assessment,
                    (int)SightingTypeSearchGroup.Aggregated,
                    (int)SightingTypeSearchGroup.AssessmentChild,
                    (int)SightingTypeSearchGroup.Replacement,
                    (int)SightingTypeSearchGroup.OwnBreedingAssessment };
            }
            else if (filter.TypeFilter == SearchFilterBase.SightingTypeFilter.ShowOnlyMerged)
            {
                sightingTypeSearchGroupFilter = new[] { (int)SightingTypeSearchGroup.Assessment }; // 2
            }
            else if (filter.TypeFilter == SearchFilterBase.SightingTypeFilter.DoNotShowSightingsInMerged)
            {
                sightingTypeSearchGroupFilter = new[] { // 1, 2, 4, 32, 128
                    (int)SightingTypeSearchGroup.Ordinary,
                    (int)SightingTypeSearchGroup.Assessment,
                    (int)SightingTypeSearchGroup.Aggregated,
                    (int)SightingTypeSearchGroup.Replacement,
                    (int)SightingTypeSearchGroup.OwnBreedingAssessment };
            }

            sightingTypeQuery.TryAddTermsCriteria("artportalenInternal.sightingTypeSearchGroupId", sightingTypeSearchGroupFilter);

            if (filter.TypeFilter != SearchFilterBase.SightingTypeFilter.ShowOnlyMerged)
            {
                // Get observations from other than Artportalen too
                sightingTypeQuery.AddNotExistsCriteria("artportalenInternal.sightingTypeSearchGroupId");
            }

            query.Add(q => q
                .Bool(b => b
                    .Should(sightingTypeQuery)
                )
            );
        }

        /// <summary>
        /// Add determination filters
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddDeterminationFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, SearchFilterBase filter)
        {
            switch (filter.DeterminationFilter)
            {
                case SightingDeterminationFilter.NotUnsureDetermination:
                    query.TryAddTermCriteria("identification.uncertainIdentification", false);
                    break;
                case SightingDeterminationFilter.OnlyUnsureDetermination:
                    query.TryAddTermCriteria("identification.uncertainIdentification", true);
                    break;
            }
        }

        /// <summary>
        /// Add geometry filter to query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="geographicsFilter"></param>
        private static void TryAddGeometryFilters(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query,
            GeographicsFilter geographicsFilter)
        {
            if (geographicsFilter == null)
            {
                return;
            }

            var boundingBoxContainers = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            if (!(!geographicsFilter.UsePointAccuracy && geographicsFilter.UseDisturbanceRadius))
            {
                boundingBoxContainers.TryAddBoundingBoxCriteria(
                    $"location.{(geographicsFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}",
                    geographicsFilter.BoundingBox);
            }

            if (geographicsFilter.UseDisturbanceRadius)
            {
                // Add both point and pointWithDisturbanceBuffer, since pointWithDisturbanceBuffer can be null if no dist buffer exists
                boundingBoxContainers.TryAddBoundingBoxCriteria(
                    "location.point",
                    geographicsFilter.BoundingBox);

                boundingBoxContainers.TryAddBoundingBoxCriteria(
                    "location.pointWithDisturbanceBuffer",
                    geographicsFilter.BoundingBox);
            }

            if (boundingBoxContainers.Any())
            {
                query.Add(q => q
                    .Bool(b => b
                        .Should(boundingBoxContainers)
                    )
                );
            }

            if (!geographicsFilter?.IsValid ?? true)
            {
                return;
            }

            var geometryContainers = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            foreach (var geom in geographicsFilter.Geometries)
            {
                switch (geom.Type.ToLower())
                {
                    case "point":
                        geometryContainers.AddGeoDistanceCriteria($"location.{(geographicsFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}", geom, GeoDistanceType.Arc, geographicsFilter.MaxDistanceFromPoint ?? 0);

                        if (!geographicsFilter.UseDisturbanceRadius)
                        {
                            continue;
                        }
                        geometryContainers.AddGeoDistanceCriteria("location.pointWithDisturbanceBuffer", geom, GeoDistanceType.Arc, geographicsFilter.MaxDistanceFromPoint ?? 0);
                        break;
                    case "polygon":
                    case "multipolygon":
                        var vaildGeometry = geom.TryMakeValid();
                        geometryContainers.AddGeoShapeCriteria($"location.{(geographicsFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}", vaildGeometry, geographicsFilter.UsePointAccuracy ? GeoShapeRelation.Intersects : GeoShapeRelation.Within);
                        if (!geographicsFilter.UseDisturbanceRadius)
                        {
                            continue;
                        }
                        geometryContainers.AddGeoShapeCriteria("location.pointWithDisturbanceBuffer", vaildGeometry, GeoShapeRelation.Intersects);
                        break;
                }
            }

            query.Add(q => q
                .Bool(b => b
                    .Should(geometryContainers)
                )
            );
        }

        private static void TryAddModifiedDateFilter(this
                ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, ModifiedDateFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            query.TryAddDateRangeCriteria("modified", filter.From, RangeTypes.GreaterThanOrEquals);
            query.TryAddDateRangeCriteria("modified", filter.To, RangeTypes.LessThanOrEquals);
        }

        /// <summary>
        /// Try to add geographic filter
        /// </summary>
        /// <param name="query"></param>
        /// <param name="geographicAreasFilter"></param>
        private static void TryAddGeographicalAreaFilter(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query,
            GeographicAreasFilter geographicAreasFilter)
        {
            if (geographicAreasFilter == null)
            {
                return;
            }

            query.TryAddTermsCriteria("artportalenInternal.birdValidationAreaIds", geographicAreasFilter.BirdValidationAreaIds);
            query.TryAddTermsCriteria("location.county.featureId", geographicAreasFilter.CountyIds);
            query.TryAddTermsCriteria("location.municipality.featureId", geographicAreasFilter.MunicipalityIds);
            query.TryAddTermsCriteria("location.parish.featureId", geographicAreasFilter.ParishIds);
            query.TryAddTermsCriteria("location.province.featureId", geographicAreasFilter.ProvinceIds);

            query.TryAddGeometryFilters(geographicAreasFilter.GeometryFilter);
        }

        private static void TryAddLocationFilter(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query,
            LocationFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            query.TryAddGeographicalAreaFilter(filter.AreaGeographic);
            query.TryAddGeometryFilters(filter.Geometries);
            query.TryAddNumericRangeCriteria("location.coordinateUncertaintyInMeters", filter.MaxAccuracy, SearchExtensionsGeneric.RangeTypes.LessThanOrEquals);

        }

        /// <summary>
        /// Try to add not recovered filter
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddNotRecoveredFilter(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, SearchFilterBase filter)
        {
            switch (filter.NotRecoveredFilter)
            {
                case SightingNotRecoveredFilter.DontIncludeNotRecovered:
                    query.TryAddTermCriteria("occurrence.isNotRediscoveredObservation", false);
                    break;
                case SightingNotRecoveredFilter.OnlyNotRecovered:
                    query.TryAddTermCriteria("occurrence.isNotRediscoveredObservation", true);
                    break;
            }
        }

        /// <summary>
        /// Try to add taxon search criteria
        /// </summary>
        /// <typeparam name="TQueryContainer"></typeparam>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddTaxonCriteria<TQueryContainer>(
            this ICollection<Func<QueryContainerDescriptor<TQueryContainer>, QueryContainer>> query, TaxonFilter filter) where TQueryContainer : class
        {
            if (filter == null)
            {
                return;
            }

            query.TryAddTermsCriteria("taxon.attributes.redlistCategory", filter.RedListCategories?.Select(m => m.ToUpper()));
            query.TryAddTermsCriteria("taxon.id", filter.Ids);
            query.TryAddTermsCriteria("occurrence.sex.id", filter.SexIds);
        }

        private static void TryAddValidationStatusFilter(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, SearchFilterBase filter)
        {
            switch (filter.VerificationStatus)
            {
                case SearchFilterBase.StatusVerification.Verified:
                    query.TryAddTermCriteria("identification.verified", true, true);
                    break;
                case SearchFilterBase.StatusVerification.NotVerified:
                    query.TryAddTermCriteria("identification.verified", false, false);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aggregationType"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static void AddAggregationFilter(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, AggregationType aggregationType)
        {
            if (aggregationType.IsDateHistogram())
            {
                // Do only include sightings whose period don't exceeds one week/year
                var maxDuration = aggregationType switch
                {
                    AggregationType.QuantityPerWeek => 7,
                    AggregationType.SightingsPerWeek => 7,
                    AggregationType.QuantityPerYear => 365,
                    AggregationType.SightingsPerYear => 365,
                    _ => 365
                };

                query.AddScript($@" (doc['event.endDate'].value.toInstant().toEpochMilli() - doc['event.startDate'].value.toInstant().toEpochMilli()) / 1000 / 86400 < {maxDuration} ");
            }

            if (aggregationType.IsSpeciesSightingsList())
            {
                query.TryAddTermsCriteria("artportalenInternal.sightingTypeId", new[] { 0, 1, 3, 8, 10 });
            }
        }

        /// <summary>
        /// Create multimedia query
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> ToMultimediaQuery(
            this SearchFilterBase filter)
        {
            var query = filter.ToQuery();
            query.AddNestedMustExistsCriteria("occurrence.media");
            return query;
        }

        public static ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> ToMeasurementOrFactsQuery(
            this SearchFilterBase filter)
        {
            var query = filter.ToQuery();
            query.AddNestedMustExistsCriteria("measurementOrFacts");
            return query;
        }

        /// <summary>
        /// Add signal search specific arguments
        /// </summary>
        /// <param name="query"></param>
        /// <param name="extendedAuthorizations"></param>
        /// <param name="onlyAboveMyClearance"></param>
        public static void AddSignalSearchCriteria(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, IEnumerable<ExtendedAuthorizationAreaFilter> extendedAuthorizations, bool onlyAboveMyClearance)
        {
            if (!extendedAuthorizations?.Any() ?? true)
            {
                return;
            }

            var protectedQuerys = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            // Allow protected observations matching user extended authorization
            foreach (var extendedAuthorization in extendedAuthorizations)
            {
                var protectedQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();
                if (onlyAboveMyClearance)
                {
                    protectedQuery.TryAddNumericRangeCriteria("occurrence.sensitivityCategory", extendedAuthorization.MaxProtectionLevel, SearchExtensionsGeneric.RangeTypes.GreaterThan);
                }

                TryAddGeographicalAreaFilter(protectedQuery, extendedAuthorization.GeographicAreas);

                protectedQuerys.Add(q => q
                    .Bool(b => b
                        .Filter(protectedQuery)
                    )
                );
            }

            query.Add(q => q
                .Bool(b => b
                    .Should(protectedQuerys)
                )
            );
        }

        /// <summary>
        ///     Create search filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> ToQuery(
            this SearchFilterBase filter)
        {
            var query = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            if (filter == null)
            {
                return query;
            }

            query.AddAuthorizationFilters(filter.ExtendedAuthorization);

            // If internal filter is "Use Period For All Year" we cannot apply date-range filter.
            if (!(filter is SearchFilterInternal filterInternal && filterInternal.UsePeriodForAllYears))
            {
                query.TryAddDateRangeFilters(filter.Date, "event.startDate", "event.endDate");
            }

            query.TryAddTimeRangeFilters(filter.Date, "event.startDate");
            query.TryAddDeterminationFilters(filter);
            query.TryAddLocationFilter(filter.Location);
            query.TryAddModifiedDateFilter(filter.ModifiedDate);
            query.TryAddNotRecoveredFilter(filter);
            query.AddSightingTypeFilters(filter);
            query.TryAddValidationStatusFilter(filter);
            query.TryAddTaxonCriteria(filter.Taxa);

            query.TryAddTermsCriteria("diffusionStatus", filter.DiffusionStatuses?.Select(ds => (int)ds));
            query.TryAddTermsCriteria("dataProviderId", filter.DataProviderIds);

            query.TryAddTermCriteria("occurrence.isPositiveObservation", filter.PositiveSightings);                        
            query.TryAddNestedTermsCriteria("projects", "id", filter.ProjectIds); 
            query.TryAddNumericRangeCriteria("occurrence.birdNestActivityId", filter.BirdNestActivityLimit, SearchExtensionsGeneric.RangeTypes.LessThanOrEquals);
            
            if (filter is SearchFilterInternal)
            {
                query.AddInternalFilters(filter);
            }

            return query;
        }

        /// <summary>
        /// Create a exclude query
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> ToExcludeQuery(this SearchFilterBase filter)
        {
            var query = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            if (filter.Location?.AreaGeographic?.GeometryFilter?.IsValid ?? false)
            {
                foreach (var geom in filter.Location.AreaGeographic?.GeometryFilter.Geometries)
                {
                    switch (geom.Type.ToLower())
                    {
                        case "holepolygon":
                            query.AddGeoShapeCriteria($"location.{(filter.Location.AreaGeographic.GeometryFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}", geom, GeoShapeRelation.Intersects);
                            if (!filter.Location.AreaGeographic.GeometryFilter.UseDisturbanceRadius) // Not sure this should be used here
                            {
                                continue;
                            }
                            query.AddGeoShapeCriteria("location.pointWithDisturbanceBuffer", geom, GeoShapeRelation.Intersects);

                            break;
                    }
                }
            }

            if (filter is SearchFilterInternal)
            {
                query.AddInternalExcludeFilters(filter);
            }

            return query;
        }

        /// <summary>
        ///     Build a projection string
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static Func<SourceFilterDescriptor<dynamic>, ISourceFilter> ToProjection(this IEnumerable<string> properties,
            bool isInternal)
        {
            var projection = new SourceFilterDescriptor<dynamic>();/*.Excludes(e => e
                .Field("event.endDay")
                .Field("event.endMonth")
                .Field("event.endYear")
                .Field("event.startDay")
                .Field("event.startMonth")
                .Field("event.startYear")
            );*/
            if (isInternal)
            {
                projection.Excludes(e => e
                    .Field("defects")
                    .Field("artportalenInternal.sightingTypeSearchGroupId")
                    .Field("location.point")
                    .Field("location.pointLocation")
                    .Field("location.pointWithBuffer")
                    .Field("location.pointWithDisturbanceBuffer")
                    .Field("location.isInEconomicZoneOfSweden"));
            }
            else
            {
                projection.Excludes(e => e
                    .Field("defects")
                    /*.Field("artportalenInternal.reportedByUserAlias")
                    .Field("artportalenInternal.identifiedByInternal")*/
                    .Field("artportalenInternal")
                    .Field("location.point")
                    .Field("location.pointLocation")
                    .Field("location.pointWithBuffer")
                    .Field("location.pointWithDisturbanceBuffer")
                    .Field("location.isInEconomicZoneOfSweden")
                );
            }

            if (properties?.Any() ?? false)
            {
                projection.Includes(i => i.Fields(properties.Select(p => p.ToField())));
            }

            return p => projection;
        }

        /// <summary>
        /// Create a sort descriptor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="indexNames"></param>
        /// <param name="sortBy"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        public static async Task<SortDescriptor<dynamic>> GetSortDescriptorAsync<T>(this IElasticClient client, string indexNames, string sortBy, SearchSortOrder sortOrder)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return null;
            }

            var sortDescriptor = new SortDescriptor<dynamic>();

            // Split sort string 
            var propertyNames = sortBy.Split('.');
            // Create a object of current class
            var parent = Activator.CreateInstance(typeof(T));
            var targetProperty = (PropertyInfo)null;

            // Loop throw all levels in passed sort string
            for (var i = 0; i < propertyNames.Length; i++)
            {
                // Get property info for current property
                targetProperty = parent?.GetProperty(propertyNames[i]);

                // Update property name to make sure it's in the correct case
                if (targetProperty != null)
                {
                    propertyNames[i] = targetProperty.Name.ToCamelCase();
                }

                // As long it's not the last property, it must be a sub object. Create a instance of it since it's the new parent
                if (i != propertyNames.Length - 1)
                {
                    parent = Activator.CreateInstance(targetProperty.GetPropertyType());
                }
            }

            // Target property found, get it's type
            var propertyType = targetProperty?.GetPropertyType();

            // If it's a string, add keyword in order to make the sorting work
            if (propertyType == typeof(string))
            {
                // GetFieldMappingAsync is case sensitive on field names, use updated property names to avoid errors
                sortBy = string.Join('.', propertyNames);
                var response =
                    await client.Indices.GetFieldMappingAsync(new GetFieldMappingRequest(indexNames, sortBy));

                if (response.IsValid)
                {
                    var hasKeyword = response.Indices
                        .FirstOrDefault().Value.Mappings
                        .FirstOrDefault().Value?.Mapping?.Values?
                        .Select(s => s as TextProperty)?
                        .Where(p => p?.Fields?.ContainsKey("keyword") ?? false)?
                        .Any() ?? false;
                    if (hasKeyword)
                    {
                        sortBy = $"{sortBy}.keyword";
                    }
                }
            }

            sortDescriptor.Field(sortBy,
                sortOrder == SearchSortOrder.Desc ? SortOrder.Descending : SortOrder.Ascending);

            return sortDescriptor;
        }
    }
}