﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nest;
using SOS.Lib.Enums;
using SOS.Lib.Models.Gis;
using SOS.Lib.Models.Search;

namespace SOS.Lib.Extensions
{
    /// <summary>
    /// ElasticSearch query extensions
    /// </summary>
    public static class SearchExtensions
    {
        private enum RangeTypes
        {
            GreaterThan,
            GreaterThanOrEquals,
            LessThan,
            LessThanOrEquals
        }

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
                query.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal",
                    "userServiceUserId",
                    filter.UserId);
                query.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal",
                    "viewAccess", true);
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

                    protectedQuery.TryAddTermCriteria("protected", true);
                    protectedQuery.TryAddNumericRangeCriteria("occurrence.protectionLevel", extendedAuthorization.MaxProtectionLevel, RangeTypes.LessThanOrEquals);
                    protectedQuery.TryAddTermsCriteria("taxon.id", extendedAuthorization.TaxonIds);
                    TryAddGeographicFilter(protectedQuery, extendedAuthorization.GeographicAreas);

                    authorizeQuerys.Add(q => q
                        .Bool(b => b
                            .Filter(protectedQuery)
                        )
                    );
                }
            }

            if (filter.UserId != 0)
            {
                var observedByMeQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();
                observedByMeQuery.TryAddTermCriteria("artportalenInternal.reportedByUserServiceUserId",
                    filter.UserId);

                authorizeQuerys.Add(q => q
                    .Bool(b => b
                        .Filter(observedByMeQuery)
                    )
                );

                var reportedByMeQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

                reportedByMeQuery.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal",
                    "userServiceUserId",
                    filter.UserId);
                reportedByMeQuery.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal",
                    "viewAccess", true);

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

            // No extended areas and not allowed to view own observations. Add criteria to make sure no protected observations will be returned
            query.TryAddTermCriteria("protected", false);
        }

        private static void AddGeoDistanceCriteria(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, IGeoShape geometry, GeoDistanceType distanceType, double distance)
        {
            query.Add(q => q
                .GeoDistance(gd => gd
                    .Field(field)
                    .DistanceType(distanceType)
                    .Location(geometry.ToGeoLocation())
                    .Distance(distance, DistanceUnit.Meters)
                    .ValidationMethod(GeoValidationMethod.IgnoreMalformed)
                )
            );
        }

        /// <summary>
        /// Add geo shape criteria
        /// </summary>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="geometry"></param>
        /// <param name="relation"></param>
        private static void AddGeoShapeCriteria(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, IGeoShape geometry, GeoShapeRelation relation)
        {
            query.Add(q => q
                .GeoShape(gd => gd
                    .Field(field)
                    .Shape(s => geometry)
                    .Relation(relation)
                )
            );
        }

        /// <summary>
        /// Add internal filters to query
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        private static void AddInternalFilters(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
        {
            var internalFilter = filter as SearchFilterInternal;

            query.TryAddTermCriteria("artportalenInternal.reportedByUserId", internalFilter.ReportedByUserId);
            query.TryAddTermCriteria("artportalenInternal.reportedByUserServiceUserId", internalFilter.ReportedByUserServiceUserId);
            query.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal", "id", internalFilter.ObservedByUserId);
            query.TryAddNestedTermCriteria("artportalenInternal.occurrenceRecordedByInternal", "userServiceUserId", internalFilter.ObservedByUserServiceUserId);

            query.TryAddTermCriteria("institutionId.keyword", internalFilter.InstitutionId);

            if (internalFilter.OnlyWithMedia)
            {
                query.AddMustExistsCriteria("occurrence.associatedMedia");
                //    query.TryAddWildcardCriteria("occurrence.associatedMedia", "http*");
            }

            if (internalFilter.OnlyWithNotes)
            {
                query.AddMustExistsCriteria("occurrence.occurrenceRemarks");
                //  query.TryAddWildcardCriteria("occurrence.occurrenceRemarks", "?*");
            }

            query.TryAddTermCriteria("artportalenInternal.noteOfInterest", internalFilter.OnlyWithNotesOfInterest, true);
            query.TryAddTermCriteria("artportalenInternal.hasUserComments", internalFilter.OnlyWithUserComments, true);
            query.TryAddDateRangeCriteria("occurrence.reportedDate", internalFilter.ReportedDateFrom, RangeTypes.GreaterThanOrEquals);
            query.TryAddDateRangeCriteria("occurrence.reportedDate", internalFilter.ReportedDateTo, RangeTypes.LessThanOrEquals);

            if (internalFilter.Months?.Any() ?? false)
            {
                string monthStartDateScript = $@"
                                    ZonedDateTime convertedStartDate = doc['event.startDate'].value.withZoneSameInstant(ZoneId.of('Europe/Stockholm'));
                                    return [{string.Join(',', internalFilter.Months.Select(m => $"{m}"))}].contains(convertedStartDate.getMonthValue());";
                string monthEndDateScript = $@"
                                    ZonedDateTime convertedStartDate = doc['event.endDate'].value.withZoneSameInstant(ZoneId.of('Europe/Stockholm'));
                                    return [{string.Join(',', internalFilter.Months.Select(m => $"{m}"))}].contains(convertedStartDate.getMonthValue());";
                if (internalFilter.MonthsComparison == MonthsFilterComparison.StartDate)
                {
                    query.AddScript(monthStartDateScript);
                }
                else if (internalFilter.MonthsComparison == MonthsFilterComparison.EndDate)
                {
                    query.AddScript(monthEndDateScript);
                }
                else if (internalFilter.MonthsComparison == MonthsFilterComparison.BothStartDateAndEndDate)
                {
                    query.AddScript(monthStartDateScript);
                    query.AddScript(monthEndDateScript);
                }
            }

            query.TryAddTermsCriteria("event.discoveryMethod.id", internalFilter.DiscoveryMethodIds);
            query.TryAddTermsCriteria("occurrence.lifeStage.id", internalFilter.LifeStageIds);
            query.TryAddTermsCriteria("occurrence.activity.id", internalFilter.ActivityIds);

            query.TryAddTermCriteria("artportalenInternal.hasTriggeredValidationRules", internalFilter.HasTriggerdValidationRule, true);
            query.TryAddTermCriteria("artportalenInternal.hasAnyTriggeredValidationRuleWithWarning", internalFilter.HasTriggerdValidationRuleWithWarning, true);


            if (internalFilter.Length.HasValue && !string.IsNullOrWhiteSpace(internalFilter.LengthOperator))
            {
                AddNumericFilterWithRelationalOperator(query, "occurrence.length", internalFilter.Length.Value, internalFilter.LengthOperator);
            }

            if (internalFilter.Weight.HasValue && !string.IsNullOrWhiteSpace(internalFilter.WeightOperator))
            {
                AddNumericFilterWithRelationalOperator(query, "occurrence.weight", internalFilter.Weight.Value, internalFilter.WeightOperator);
            }

            if (internalFilter.Quantity.HasValue && !string.IsNullOrWhiteSpace(internalFilter.QuantityOperator))
            {
                AddNumericFilterWithRelationalOperator(query, "occurrence.organismQuantityInt", internalFilter.Quantity.Value, internalFilter.QuantityOperator);
            }

            query.TryAddTermsCriteria("identification.validationStatus.id", internalFilter.ValidationStatusIds);

            if (internalFilter.OnlyWithBarcode)
            {
                query.AddMustExistsCriteria("taxon.individualId");
                // query.TryAddWildcardCriteria("taxon.individualId", "?*");
            }

            switch (internalFilter.UnspontaneousFilter)
            {
                case SightingUnspontaneousFilter.NotUnspontaneous:
                    query.TryAddTermCriteria("occurrence.isNaturalOccurrence", true);
                    break;
                case SightingUnspontaneousFilter.Unspontaneous:
                    query.TryAddTermCriteria("occurrence.isNaturalOccurrence", false);
                    break;
            }

            query.TryAddTermCriteria("speciesCollectionLabel.keyword", internalFilter.SpeciesCollectionLabel);
            query.TryAddTermCriteria("publicCollection.keyword", internalFilter.PublicCollection);
            query.TryAddTermCriteria("privateCollection.keyword", internalFilter.PrivateCollection);
            query.TryAddTermCriteria("occurrence.substrate.speciesId", internalFilter.SubstrateSpeciesId);
            query.TryAddTermCriteria("occurrence.substrate.id", internalFilter.SubstrateId);
            query.TryAddTermCriteria("event.biotope.id", internalFilter.BiotopeId);
            
            switch (internalFilter.NotPresentFilter)
            {
                case SightingNotPresentFilter.DontIncludeNotPresent:
                    query.TryAddTermCriteria("occurrence.isNeverFoundObservation", false);
                    break;
                case SightingNotPresentFilter.OnlyNotPresent:
                    query.TryAddTermCriteria("occurrence.isNeverFoundObservation", true);
                    break;
            }

            if (internalFilter.OnlySecondHandInformation)
            {
                query.TryAddWildcardCriteria("occurrence.recordedBy", "Via*");
                query.AddScript("doc['reportedByUserId'].value ==  doc['occurrence.recordedByInternal.id'].value");
            }

            query.TryAddTermsCriteria("artportalenInternal.regionalSightingStateId", internalFilter.RegionalSightingStateIdsFilter);
            query.TryAddTermsCriteria("artportalenInternal.sightingPublishTypeIds", internalFilter.PublishTypeIdsFilter);

            //search by locationId, but include child-locations observations aswell

            var siteTerms = internalFilter?.SiteIds?.Select(s => $"urn:lsid:artportalen.se:site:{s}");
            if (siteTerms?.Any() ?? false)
            {
                
                query.Add(q => q
                    .Bool(p=>p
                        .Should(
                            s=>s
                            .Terms(t=> t
                                .Field("location.locationId")
                                .Terms(siteTerms)),
                            s =>s
                            .Terms(t => t
                                .Field("artportalenInternal.parentLocationId")
                                .Terms(internalFilter.SiteIds))
                             )));
            }

            if (internalFilter.SpeciesFactsIds?.Any() ?? false)
            {
                foreach (var factsId in internalFilter.SpeciesFactsIds)
                {
                    query.TryAddTermCriteria("artportalenInternal.speciesFactsIds", factsId);
                }
            }

            if (internalFilter.UsePeriodForAllYears && internalFilter.StartDate.HasValue && internalFilter.EndDate.HasValue)
            {
                var selector = "";
               
                if (filter.DateFilterType == FilterBase.DateRangeFilterType.BetweenStartDateAndEndDate)
                {
                    selector = "((startMonth > fromMonth || (startMonth == fromMonth && startDay >= fromDay)) && (endMonth < toMonth || (endMonth == toMonth && endDay <= toDay)))";
                }
                else if (filter.DateFilterType == FilterBase.DateRangeFilterType.OnlyStartDate)
                {
                    selector = "((startMonth > fromMonth || (startMonth == fromMonth && startDay >= fromDay)) && (startMonth < toMonth || (startMonth == toMonth && startDay <= toDay)))";
                }
                else if (filter.DateFilterType == FilterBase.DateRangeFilterType.OnlyEndDate)
                {
                    selector = "((endMonth > fromMonth || (endMonth == fromMonth && endDay >= fromDay)) && (endMonth < toMonth || (endMonth == toMonth && endDay <= toDay)))";
                }
                else if (filter.DateFilterType == FilterBase.DateRangeFilterType.OverlappingStartDateAndEndDate)
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

                            int fromMonth = {internalFilter.StartDate.Value.Month};
                            int fromDay = {internalFilter.StartDate.Value.Day};
                            int toMonth = {internalFilter.EndDate.Value.Month};
                            int toDay = {internalFilter.EndDate.Value.Day};

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

            query.TryAddTermsCriteria("artportalenInternal.datasourceId", internalFilter.DatasourceIds);

            query.TryAddWildcardCriteria("location.locality", internalFilter.Location.NameFilter);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="excludeQuery"></param>
        /// <returns></returns>
        private static void AddInternalExcludeFilters(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> excludeQuery, FilterBase filter)
        {
            var internalFilter = filter as SearchFilterInternal;

            excludeQuery.TryAddTermsCriteria("identification.validationStatus.id", internalFilter.ExcludeValidationStatusIds);
        }

        /// <summary>
            /// Add field must exists criteria
            /// </summary>
            /// <param name="query"></param>
            /// <param name="field"></param>
            private static void AddMustExistsCriteria(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field)
        {
            query.Add(q => q
                .Regexp(re => re.Field(field).Value(".+"))
            );
        }

        private static void AddNestedMustExistsCriteria(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string path)
        {
            query.Add(q => q
                .Nested(n => n
                    .Path(path)
                    .Query(nq => nq
                        .Bool(b => b
                            .Filter(f => f
                                .Exists(e => e
                                    .Field(path)
                                )
                            )
                        )
                    )
                )
            );
        }

        /// <summary>
        /// Add field not exists criteria
        /// </summary>
        /// <param name="query"></param>
        /// <param name="field"></param>
        private static void AddNotExistsCriteria(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field)
        {
            query.Add(q => q
                .Bool(b => b
                    .MustNot(mn => mn
                        .Exists(e => e
                            .Field(field)
                        )
                    )
                )
            );
        }

        // Get observations from other than Artportalen too

        /// <summary>
        /// Add numeric filter with relation operator
        /// </summary>
        /// <param name="queryInternal"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="relationalOperator"></param>
        private static void AddNumericFilterWithRelationalOperator(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> queryInternal, string fieldName,
            int value, string relationalOperator)
        {
            switch (relationalOperator.ToLower())
            {
                case "eq":
                    queryInternal.Add(q => q
                        .Term(r => r
                            .Field(fieldName)
                            .Value(value)
                        )
                    );
                    break;
                case "gte":
                    queryInternal.Add(q => q
                        .Range(r => r
                            .Field(fieldName)
                            .GreaterThanOrEquals(value)
                        )
                    );
                    break;
                case "lte":
                    queryInternal.Add(q => q
                        .Range(r => r
                            .Field(fieldName)
                            .LessThanOrEquals(value)
                        )
                    );
                    break;
            }
        }

        /// <summary>
        /// Add script source
        /// </summary>
        /// <param name="query"></param>
        /// <param name="source"></param>
        private static void AddScript(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string source)
        {
            query.Add(q => q
                .Script(s => s
                    .Script(sc => sc
                        .Source(source)
                    )
                )
            );
        }

        private static void AddSightingTypeFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
        {
            var sightingTypeQuery = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            // Default DoNotShowMerged
            var sightingTypeSearchGroupFilter = new[] { 0, 1, 4, 16, 32, 128 };

            if (filter.TypeFilter == SearchFilterInternal.SightingTypeFilter.ShowBoth)
            {
                sightingTypeSearchGroupFilter = new[] { 0, 1, 2, 4, 16, 32, 128 };
            }
            else if (filter.TypeFilter == SearchFilterInternal.SightingTypeFilter.ShowOnlyMerged)
            {
                sightingTypeSearchGroupFilter = new[] { 0, 2 };
            }
            else if (filter.TypeFilter == SearchFilterInternal.SightingTypeFilter.DoNotShowSightingsInMerged)
            {
                sightingTypeSearchGroupFilter = new[] { 0, 1, 2, 4, 32, 128 };
            }

            sightingTypeQuery.TryAddTermsCriteria("artportalenInternal.sightingTypeSearchGroupId", sightingTypeSearchGroupFilter);

            if (filter.TypeFilter != SearchFilterInternal.SightingTypeFilter.ShowOnlyMerged)
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
        /// Cast property to field
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Field ToField(this string property)
        {
            return new Field(string.Join('.', property.Split('.').Select(p => p
                .ToCamelCase()
            )));
        }

        /// <summary>
        /// Try to add bounding box criteria
        /// </summary>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="boundingBox"></param>
        private static void TryAddBoundingBoxCriteria(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, LatLonBoundingBox boundingBox)
        {
            if (boundingBox?.BottomRight == null || boundingBox?.TopLeft == null)
            {
                return;
            }

            query.Add(q => q
                .GeoBoundingBox(g => g
                    .Field(new Field(field))
                    .BoundingBox(
                        boundingBox.TopLeft.Latitude,
                        boundingBox.TopLeft.Longitude,
                        boundingBox.BottomRight.Latitude,
                        boundingBox.BottomRight.Longitude)
                )
            );
        }

        /// <summary>
        /// Add determination filters
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddDeterminationFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
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
        /// <param name="geometryFilter"></param>
        private static void TryAddGeometryFilters(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query,
            GeographicsFilter geometryFilter)
        {
            if (geometryFilter == null)
            {
                return;
            }

            var boundingBoxContainers = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

            if (!(!geometryFilter.UsePointAccuracy && geometryFilter.UseDisturbanceRadius))
            {
                boundingBoxContainers.TryAddBoundingBoxCriteria(
                    $"location.{(geometryFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}",
                    geometryFilter.BoundingBox);
            }
            
            if (geometryFilter.UseDisturbanceRadius)
            {
                // Add both point and pointWithDisturbanceBuffer, since pointWithDisturbanceBuffer can be null if no dist buffer exists
                boundingBoxContainers.TryAddBoundingBoxCriteria(
                    "location.point",
                    geometryFilter.BoundingBox);

                boundingBoxContainers.TryAddBoundingBoxCriteria(
                    "location.pointWithDisturbanceBuffer",
                    geometryFilter.BoundingBox);
            }

            if (boundingBoxContainers.Any())
            {
                query.Add(q => q
                    .Bool(b => b
                        .Should(boundingBoxContainers)
                    )
                );
            }

            if (geometryFilter?.IsValid ?? false)
            {
                var geometryContainers = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();

                foreach (var geom in geometryFilter.Geometries)
                {
                    switch (geom.Type.ToLower())
                    {
                        case "point":
                            geometryContainers.AddGeoDistanceCriteria($"location.{(geometryFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}", geom, GeoDistanceType.Arc, geometryFilter.MaxDistanceFromPoint ?? 0);

                            if (!geometryFilter.UseDisturbanceRadius)
                            {
                                continue;
                            }
                            geometryContainers.AddGeoDistanceCriteria("location.pointWithDisturbanceBuffer", geom, GeoDistanceType.Arc, geometryFilter.MaxDistanceFromPoint ?? 0);
                            break;
                        case "polygon":
                        case "multipolygon":
                            var vaildGeometry = geom.TryMakeValid();
                            geometryContainers.AddGeoShapeCriteria($"location.{(geometryFilter.UsePointAccuracy ? "pointWithBuffer" : "point")}", vaildGeometry, geometryFilter.UsePointAccuracy ? GeoShapeRelation.Intersects : GeoShapeRelation.Within);
                            if (!geometryFilter.UseDisturbanceRadius)
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
        }

        /// <summary>
        /// Try to add nested term criteria
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="nestedPath"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        private static void TryAddNestedTermCriteria<T>(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string nestedPath, string field, T value)
        {
            query.Add(q => q
                .Nested(n => n
                    .Path(nestedPath)
                    .Query(q => q
                        .Term(t => t
                            .Field($"{nestedPath}.{field}")
                            .Value(value)
                        )
                    )));
        }

        /// <summary>
        /// Try to add nested terms criteria
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="nestedPath"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        private static void TryAddNestedTermsCriteria<T>(this
            ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string nestedPath, string field, IEnumerable<T> values)
        {
            if (values?.Any() ?? false)
            {
                query.Add(q => q
                    .Nested(n => n
                        .Path(nestedPath)
                        .Query(q => q
                            .Terms(t => t
                                .Field($"{nestedPath}.{field}")
                                .Terms(values)
                            )
                        )));
            }
        }

        /// <summary>
        /// Add numeric range criteria if value is not null 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        private static void TryAddNumericRangeCriteria(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, double? value, RangeTypes type)
        {
            if (value.HasValue)
            {
                switch (type)
                {
                    case RangeTypes.GreaterThan:
                        query.Add(q => q
                            .Range(r => r
                                .Field(field)
                                .GreaterThan(value)
                            )
                        );
                        break;
                    case RangeTypes.GreaterThanOrEquals:
                        query.Add(q => q
                            .Range(r => r
                                .Field(field)
                                .GreaterThanOrEquals(value)
                            )
                        );
                        break;
                    case RangeTypes.LessThan:
                        query.Add(q => q
                            .Range(r => r
                                .Field(field)
                                .LessThan(value)
                            )
                        );
                        break;
                    case RangeTypes.LessThanOrEquals:
                        query.Add(q => q
                            .Range(r => r
                                .Field(field)
                                .LessThanOrEquals(value)
                            )
                        );
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="dateTime"></param>
        /// <param name="type"></param>
        private static void TryAddDateRangeCriteria(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, DateTime? dateTime, RangeTypes type)
        {
            if (dateTime.HasValue)
            {
                switch (type)
                {
                    case RangeTypes.GreaterThan:
                        query.Add(q => q
                            .DateRange(r => r
                                .Field(field)
                                .GreaterThan(
                                    DateMath.Anchored(
                                        dateTime.Value.ToUniversalTime()
                                    )
                                )
                            )
                        );
                        break;
                    case RangeTypes.GreaterThanOrEquals:
                        query.Add(q => q
                            .DateRange(r => r
                                .Field(field)
                                .GreaterThanOrEquals(
                                    DateMath.Anchored(
                                        dateTime.Value.ToUniversalTime()
                                    )
                                )
                            )
                        );
                        break;
                    case RangeTypes.LessThan:
                        query.Add(q => q
                            .DateRange(r => r
                                .Field(field)
                                .LessThan(
                                    DateMath.Anchored(
                                        dateTime.Value.ToUniversalTime()
                                    )
                                )
                            )
                        );
                        break;
                    case RangeTypes.LessThanOrEquals:
                        query.Add(q => q
                            .DateRange(r => r
                                .Field(field)
                                .LessThanOrEquals(
                                    DateMath.Anchored(
                                        dateTime.Value.ToUniversalTime()
                                    )
                                )
                            )
                        );
                        break;
                }
            }

        }

        /// <summary>
        /// Add date range query to filter
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddDateRangeFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
        {
            if (filter.DateFilterType == FilterBase.DateRangeFilterType.BetweenStartDateAndEndDate)
            {
                query.TryAddDateRangeCriteria("event.startDate", filter.StartDate, RangeTypes.GreaterThanOrEquals);
                query.TryAddDateRangeCriteria("event.endDate", filter.EndDate, RangeTypes.LessThanOrEquals);
            }
            else if (filter.DateFilterType == FilterBase.DateRangeFilterType.OverlappingStartDateAndEndDate)
            {
                query.TryAddDateRangeCriteria("event.startDate", filter.EndDate, RangeTypes.LessThanOrEquals);
                query.TryAddDateRangeCriteria("event.endDate", filter.StartDate, RangeTypes.GreaterThanOrEquals);

            }
            else if (filter.DateFilterType == FilterBase.DateRangeFilterType.OnlyStartDate)
            {
                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    query.TryAddDateRangeCriteria("event.startDate", filter.StartDate, RangeTypes.GreaterThanOrEquals);
                    query.TryAddDateRangeCriteria("event.startDate", filter.EndDate, RangeTypes.LessThanOrEquals);
                }
            }
            else if (filter.DateFilterType == FilterBase.DateRangeFilterType.OnlyEndDate)
            {
                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    query.TryAddDateRangeCriteria("event.endDate", filter.StartDate, RangeTypes.GreaterThanOrEquals);
                    query.TryAddDateRangeCriteria("event.endDate", filter.EndDate, RangeTypes.LessThanOrEquals);
                }
            }
        }


        /// <summary>
        /// Try to add geographic filter
        /// </summary>
        /// <param name="query"></param>
        /// <param name="geographicFilter"></param>
        private static void TryAddGeographicFilter(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query,
            GeographicAreasFilter geographicFilter)
        {
            if (geographicFilter == null)
            {
                return;
            }

            query.TryAddTermsCriteria("artportalenInternal.birdValidationAreaIds", geographicFilter.BirdValidationAreaIds);
            query.TryAddTermsCriteria("location.county.featureId", geographicFilter.CountyIds);
            query.TryAddTermsCriteria("location.municipality.featureId", geographicFilter.MunicipalityIds);
            query.TryAddTermsCriteria("location.parish.featureId", geographicFilter.ParishIds);
            query.TryAddTermsCriteria("location.province.featureId", geographicFilter.ProvinceIds);

            query.TryAddGeometryFilters(geographicFilter.GeometryFilter);
        }

        /// <summary>
        /// Try to add not recovered filter
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddNotRecoveredFilter(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
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
        /// Try to add query criteria
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="terms"></param>
        public static void TryAddTermsCriteria<T>(
                    this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, IEnumerable<T> terms)
        {
            if (terms?.Any() ?? false)
            {
                query.Add(q => q
                    .Terms(t => t
                        .Field(field)
                        .Terms(terms)
                    )
                );
            }
        }

        /// <summary>
        /// Try to add query criteria
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public static void TryAddTermCriteria<T>(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, T value)
        {
            if (!string.IsNullOrEmpty(value?.ToString()))
            {
                query.Add(q => q
                    .Term(m => m.Field(field).Value(value))); // new Field(field)
            }
        }

        /// <summary>
        /// Try to add query criteria where property must match a specified value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="matchValue"></param>
        private static void TryAddTermCriteria<T>(
            this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, T value, T matchValue)
        {
            if (!string.IsNullOrEmpty(value?.ToString()) && matchValue.Equals(value))
            {
                query.Add(q => q
                    .Term(m => m.Field(field).Value(value)));
            }
        }

        /// <summary>
        /// Add time range filters
        /// </summary>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        private static void TryAddTimeRangeFilters(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
        {
            if (!filter.TimeRanges?.Any() ?? true)
            {
                return;
            }

            var timeRangeContainers = new List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>>();
            foreach (var timeRange in filter.TimeRanges)
            {
                switch (timeRange)
                {
                    case FilterBase.TimeRange.Morning:
                        timeRangeContainers.AddScript($@"[4, 5, 6, 7, 8].contains(doc['event.startDate'].value.getHour())");
                        break;
                    case FilterBase.TimeRange.Forenoon:
                        timeRangeContainers.AddScript($@"[9, 10, 11, 12].contains(doc['event.startDate'].value.getHour())");
                        break;
                    case FilterBase.TimeRange.Afternoon:
                        timeRangeContainers.AddScript($@"[13, 14, 15, 16, 17].contains(doc['event.startDate'].value.getHour())");
                        break;
                    case FilterBase.TimeRange.Evening:
                        timeRangeContainers.AddScript($@"[18, 19, 20, 21, 22].contains(doc['event.startDate'].value.getHour())");
                        break;
                    default:
                        timeRangeContainers.AddScript($@"[23, 0, 1, 2, 3].contains(doc['event.startDate'].value.getHour())");
                        break;
                }
            }

            query.Add(q => q
                .Bool(b => b
                    .Should(timeRangeContainers)
                )
            );
        }

        private static void TryAddValidationStatusFilter(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, FilterBase filter)
        {
            switch (filter.ValidationStatus)
            {
                case FilterBase.StatusValidation.Validated:
                    query.TryAddTermCriteria("identification.validated", true, true);
                    break;
                case FilterBase.StatusValidation.NotValidated:
                    query.TryAddTermCriteria("identification.validated", false, false);
                    break;
            }
        }

        /// <summary>
        /// Add wildcard criteria
        /// </summary>
        /// <param name="query"></param>
        /// <param name="field"></param>
        /// <param name="wildcard"></param>
        private static void TryAddWildcardCriteria(this ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> query, string field, string wildcard)
        {
            if (string.IsNullOrEmpty(wildcard))
            {
                return;
            }

            query.Add(q => q
                .Wildcard(w => w
                    .Field(field)
                    .Value(wildcard)
                )
            );
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
            this FilterBase filter)
        {
            var query = filter.ToQuery();
            query.AddNestedMustExistsCriteria("occurrence.media");
            return query;
        }

        public static ICollection<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> ToMeasurementOrFactsQuery(
            this FilterBase filter)
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
                    protectedQuery.TryAddNumericRangeCriteria("occurrence.protectionLevel", extendedAuthorization.MaxProtectionLevel, RangeTypes.GreaterThan);
                }

                TryAddGeographicFilter(protectedQuery, extendedAuthorization.GeographicAreas);

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
            this FilterBase filter)
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
                query.TryAddDateRangeFilters(filter);
            }

            query.TryAddDeterminationFilters(filter);
            query.TryAddTimeRangeFilters(filter);
            query.TryAddGeographicFilter(filter.Location?.AreaGeographic);
            query.TryAddGeometryFilters(filter.Location?.Geometries);
            query.TryAddNotRecoveredFilter(filter);
            query.AddSightingTypeFilters(filter);
            query.TryAddValidationStatusFilter(filter);

            query.TryAddTermsCriteria("diffusionStatus", filter.DiffusionStatuses?.Select(ds => (int)ds));
            query.TryAddTermsCriteria("dataProviderId", filter.DataProviderIds);

            query.TryAddTermCriteria("occurrence.isPositiveObservation", filter.PositiveSightings);
            query.TryAddTermsCriteria("occurrence.sex.id", filter.SexIds);
            query.TryAddTermsCriteria("taxon.attributes.redlistCategory", filter.Taxa?.RedListCategories?.Select(m => m.ToLower()));
            query.TryAddTermsCriteria("taxon.id", filter.Taxa?.Ids);
            query.TryAddNestedTermsCriteria("projects", "id", filter.ProjectIds);
            query.TryAddNumericRangeCriteria("location.coordinateUncertaintyInMeters", filter.Location?.MaxAccuracy, RangeTypes.LessThanOrEquals);
            query.TryAddNumericRangeCriteria("occurrence.birdNestActivityId", filter.BirdNestActivityLimit, RangeTypes.LessThanOrEquals);

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
        public static List<Func<QueryContainerDescriptor<dynamic>, QueryContainer>> ToExcludeQuery(this FilterBase filter)
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
            var projection = new SourceFilterDescriptor<dynamic>();
            if (isInternal)
            {
                projection.Excludes(e => e
                    .Field("defects")
                    .Field("artportalenInternal.sightingTypeSearchGroupId")
                    .Field("location.point")
                    .Field("location.pointLocation")
                    .Field("location.pointWithBuffer")
                    .Field("location.pointWithDisturbanceBuffer")
                    .Field("isInEconomicZoneOfSweden"));
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
                    .Field("isInEconomicZoneOfSweden")
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
        /// <param name="sortBy"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        public static SortDescriptor<dynamic> ToSortDescriptor<T>(this string sortBy, SearchSortOrder sortOrder)
        {
            var sortDescriptor = new SortDescriptor<dynamic>();

            if (!string.IsNullOrEmpty(sortBy))
            {
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
                    //check if the string property already has the keyword attribute, if it does, we do not need the .keyword
                    KeywordAttribute isKeywordAttribute =
                        (KeywordAttribute)Attribute.GetCustomAttribute(targetProperty, typeof(KeywordAttribute));
                    if (isKeywordAttribute == null)
                    {
                        sortBy = $"{sortBy}.keyword";
                    }
                }

                sortDescriptor.Field(sortBy,
                    sortOrder == SearchSortOrder.Desc ? SortOrder.Descending : SortOrder.Ascending);
            }

            return sortDescriptor;
        }
    }
}
