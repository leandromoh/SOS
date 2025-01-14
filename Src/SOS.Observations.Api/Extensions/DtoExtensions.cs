﻿using System.Collections.Generic;
using System.Linq;
using SOS.Lib.Enums;
using SOS.Lib.Extensions;
using SOS.Lib.Helpers;
using SOS.Lib.Models;
using SOS.Lib.Models.Gis;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Search;
using SOS.Lib.Models.Shared;
using SOS.Observations.Api.Dtos;
using SOS.Observations.Api.Dtos.Filter;
using SOS.Observations.Api.Dtos.Vocabulary;

namespace SOS.Observations.Api.Extensions
{
    public static class DtoExtensions
    {
        private static FilterBase PopulateFilter(SearchFilterBaseDto searchFilterBaseDto, string translationCultureCode, bool protectedObservations)
        {
            if (searchFilterBaseDto == null) return default;

            var filter = searchFilterBaseDto is SearchFilterInternalBaseDto ? new SearchFilterInternal() : new SearchFilter();

            filter.Taxa = PopulateTaxa(searchFilterBaseDto.Taxon);

            filter.StartDate = searchFilterBaseDto.Date?.StartDate;
            filter.EndDate = searchFilterBaseDto.Date?.EndDate;
            filter.DateFilterType = (FilterBase.DateRangeFilterType)(searchFilterBaseDto.Date?.DateFilterType).GetValueOrDefault();
            filter.TimeRanges = searchFilterBaseDto.Date?.TimeRanges?.Select(tr => (FilterBase.TimeRange)tr).ToList();
            filter.DataProviderIds = searchFilterBaseDto.DataProvider?.Ids;
            filter.FieldTranslationCultureCode = translationCultureCode;
            filter.NotRecoveredFilter = (SightingNotRecoveredFilter)searchFilterBaseDto.NotRecoveredFilter;
            filter.ValidationStatus = (FilterBase.StatusValidation) searchFilterBaseDto.ValidationStatus;
            filter.ProjectIds = searchFilterBaseDto.ProjectIds;
            filter.BirdNestActivityLimit = searchFilterBaseDto.BirdNestActivityLimit;
            filter.Location.Areas = searchFilterBaseDto.Geographics?.Areas?.Select(a => new AreaFilter { FeatureId = a.FeatureId, AreaType = (AreaType)a.AreaType });
            filter.Location.MaxAccuracy = searchFilterBaseDto.Geographics?.MaxAccuracy;
            filter.Location.Geometries = searchFilterBaseDto.Geographics == null
                ? null
                : new GeographicsFilter
                {
                    BoundingBox = searchFilterBaseDto.Geographics.BoundingBox?.ToLatLonBoundingBox(),
                    Geometries = searchFilterBaseDto.Geographics.Geometries?.ToList(),
                    MaxDistanceFromPoint = searchFilterBaseDto.Geographics.MaxDistanceFromPoint,
                    UseDisturbanceRadius = searchFilterBaseDto.Geographics.ConsiderDisturbanceRadius,
                    UsePointAccuracy = searchFilterBaseDto.Geographics.ConsiderObservationAccuracy
                };

            filter.ExtendedAuthorization.ProtectedObservations = protectedObservations;
            filter.ExtendedAuthorization.ObservedByMe = searchFilterBaseDto.ObservedByMe;
            filter.ExtendedAuthorization.ReportedByMe = searchFilterBaseDto.ReportedByMe;

            if (searchFilterBaseDto.OccurrenceStatus != null)
            {
                switch (searchFilterBaseDto.OccurrenceStatus)
                {
                    case OccurrenceStatusFilterValuesDto.Present:
                        filter.PositiveSightings = true;
                        break;
                    case OccurrenceStatusFilterValuesDto.Absent:
                        filter.PositiveSightings = false;
                        break;
                    case OccurrenceStatusFilterValuesDto.BothPresentAndAbsent:
                        filter.PositiveSightings = null;
                        break;
                }
            }

            filter.DiffusionStatuses = searchFilterBaseDto.DiffusionStatuses?.Select(dsd => (DiffusionStatus) dsd)?.ToList();

            filter.DeterminationFilter = (SightingDeterminationFilter)searchFilterBaseDto.DeterminationFilter;

            if (searchFilterBaseDto is SearchFilterDto searchFilterDto)
            {
                filter.OutputFields = searchFilterDto.Output?.Fields?.ToList();
                filter.PopulateOutputFields(searchFilterDto.Output?.FieldSet);
            }

            if (searchFilterBaseDto is SearchFilterInternalBaseDto searchFilterInternalBaseDto)
            {
                var filterInternal = (SearchFilterInternal)filter;
                PopulateInternalBase(searchFilterInternalBaseDto, filterInternal);

                if (searchFilterBaseDto is SearchFilterInternalDto searchFilterInternalDto)
                {
                    filterInternal.IncludeRealCount = searchFilterInternalDto.IncludeRealCount;
                    filter.OutputFields = searchFilterInternalDto.Output?.Fields?.ToList();
                    filter.PopulateOutputFields(searchFilterInternalDto.Output?.FieldSet);
                }
            }

            return filter;
        }

        private static void PopulateInternalBase(SearchFilterInternalBaseDto searchFilterInternalDto, SearchFilterInternal internalFilter)
        {
            if (searchFilterInternalDto.ExtendedFilter!= null)
            {
                internalFilter.ReportedByUserId = searchFilterInternalDto.ExtendedFilter.ReportedByUserId;
                internalFilter.ObservedByUserId = searchFilterInternalDto.ExtendedFilter.ObservedByUserId;
                internalFilter.ReportedByUserServiceUserId = searchFilterInternalDto.ExtendedFilter.ReportedByUserServiceUserId;
                internalFilter.ObservedByUserServiceUserId = searchFilterInternalDto.ExtendedFilter.ObservedByUserServiceUserId;
                internalFilter.OnlyWithMedia = searchFilterInternalDto.ExtendedFilter.OnlyWithMedia;
                internalFilter.OnlyWithNotes = searchFilterInternalDto.ExtendedFilter.OnlyWithNotes;
                internalFilter.OnlyWithNotesOfInterest = searchFilterInternalDto.ExtendedFilter.OnlyWithNotesOfInterest;
                internalFilter.OnlyWithUserComments = searchFilterInternalDto.ExtendedFilter.OnlyWithUserComments;
                internalFilter.OnlyWithBarcode = searchFilterInternalDto.ExtendedFilter.OnlyWithBarcode;
                internalFilter.ReportedDateFrom = searchFilterInternalDto.ExtendedFilter.ReportedDateFrom;
                internalFilter.ReportedDateTo = searchFilterInternalDto.ExtendedFilter.ReportedDateTo;
                internalFilter.TypeFilter = (SearchFilterInternal.SightingTypeFilter)searchFilterInternalDto.ExtendedFilter.TypeFilter;
                internalFilter.UsePeriodForAllYears = searchFilterInternalDto.ExtendedFilter.UsePeriodForAllYears;
                internalFilter.Months = searchFilterInternalDto.ExtendedFilter.Months;
                internalFilter.MonthsComparison = (MonthsFilterComparison)searchFilterInternalDto.ExtendedFilter.MonthsComparison;
                internalFilter.DiscoveryMethodIds = searchFilterInternalDto.ExtendedFilter.DiscoveryMethodIds;
                internalFilter.LifeStageIds = searchFilterInternalDto.ExtendedFilter.LifeStageIds;
                internalFilter.ActivityIds = searchFilterInternalDto.ExtendedFilter.ActivityIds;
                internalFilter.HasTriggerdValidationRule = searchFilterInternalDto.ExtendedFilter.HasTriggerdValidationRule;
                internalFilter.HasTriggerdValidationRuleWithWarning =
                    searchFilterInternalDto.ExtendedFilter.HasTriggerdValidationRuleWithWarning;
                internalFilter.Length = searchFilterInternalDto.ExtendedFilter.Length;
                internalFilter.LengthOperator = searchFilterInternalDto.ExtendedFilter.LengthOperator;
                internalFilter.Weight = searchFilterInternalDto.ExtendedFilter.Weight;
                internalFilter.WeightOperator = searchFilterInternalDto.ExtendedFilter.WeightOperator;
                internalFilter.Quantity = searchFilterInternalDto.ExtendedFilter.Quantity;
                internalFilter.QuantityOperator = searchFilterInternalDto.ExtendedFilter.QuantityOperator;
                internalFilter.ValidationStatusIds = searchFilterInternalDto.ExtendedFilter.ValidationStatusIds;
                internalFilter.ExcludeValidationStatusIds = searchFilterInternalDto.ExtendedFilter.ExcludeValidationStatusIds;
                internalFilter.UnspontaneousFilter =
                    (SightingUnspontaneousFilter)searchFilterInternalDto.ExtendedFilter
                        .UnspontaneousFilter;
                internalFilter.SpeciesCollectionLabel = searchFilterInternalDto.ExtendedFilter.SpeciesCollectionLabel;
                internalFilter.PublicCollection = searchFilterInternalDto.ExtendedFilter.PublicCollection;
                internalFilter.PrivateCollection = searchFilterInternalDto.ExtendedFilter.PrivateCollection;
                internalFilter.SubstrateSpeciesId = searchFilterInternalDto.ExtendedFilter.SubstrateSpeciesId;
                internalFilter.SubstrateId = searchFilterInternalDto.ExtendedFilter.SubstrateId;
                internalFilter.BiotopeId = searchFilterInternalDto.ExtendedFilter.BiotopeId;
                internalFilter.NotPresentFilter =
                    (SightingNotPresentFilter)searchFilterInternalDto.ExtendedFilter.NotPresentFilter;
                internalFilter.OnlySecondHandInformation = searchFilterInternalDto.ExtendedFilter.OnlySecondHandInformation;
                internalFilter.PublishTypeIdsFilter = searchFilterInternalDto.ExtendedFilter.PublishTypeIdsFilter;
                internalFilter.RegionalSightingStateIdsFilter =
                    searchFilterInternalDto.ExtendedFilter.RegionalSightingStateIdsFilter;
                internalFilter.SiteIds = searchFilterInternalDto.ExtendedFilter.SiteIds;
                internalFilter.SpeciesFactsIds = searchFilterInternalDto.ExtendedFilter.SpeciesFactsIds;
                internalFilter.SexIds = searchFilterInternalDto.ExtendedFilter.SexIds?.ToList();
                internalFilter.InstitutionId = searchFilterInternalDto.ExtendedFilter.InstitutionId;
                internalFilter.DatasourceIds = searchFilterInternalDto.ExtendedFilter.DatasourceIds;
                internalFilter.Location.NameFilter = searchFilterInternalDto.ExtendedFilter.LocationNameFilter;
            }

        }

        private static TaxonFilter PopulateTaxa(TaxonFilterBaseDto filterDto)
        {
            if (filterDto == null)
            {
                return null;
            }

            var filter = new TaxonFilter
            {
                Ids = filterDto.Ids,
                IncludeUnderlyingTaxa = filterDto.IncludeUnderlyingTaxa,
                ListIds = filterDto.TaxonListIds,
                TaxonListOperator = TaxonFilter.TaxonListOp.Merge
            };

            if (filterDto is TaxonFilterDto taxonFilterDto)
            {
                filter.RedListCategories = taxonFilterDto.RedListCategories;
                filter.TaxonListOperator =
                    (TaxonFilter.TaxonListOp) (taxonFilterDto?.TaxonListOperator).GetValueOrDefault();
            }

            return filter;
        }

        public static void OverrideBoundingBox(this SearchFilter filter, LatLonBoundingBox boundingbox)
        {
            filter ??= new SearchFilter();
            filter.Location.Geometries ??= new GeographicsFilter();
            filter.Location.Geometries.BoundingBox = boundingbox;
        }

        public static GeoGridTileTaxonPageResultDto ToGeoGridTileTaxonPageResultDto(this GeoGridTileTaxonPageResult pageResult)
        {
            return new GeoGridTileTaxonPageResultDto
            {
                NextGeoTilePage = pageResult.NextGeoTilePage,
                NextTaxonIdPage = pageResult.NextTaxonIdPage,
                HasMorePages = pageResult.HasMorePages,
                GridCells = pageResult.GridCells.Select(m => m.ToGeoGridTileTaxaCellDto())
            };
        }

        public static GeoGridTileTaxaCellDto ToGeoGridTileTaxaCellDto(this GeoGridTileTaxaCell cell)
        {
            return new GeoGridTileTaxaCellDto
            {
                BoundingBox = ToLatLonBoundingBoxDto(cell.BoundingBox),
                GeoTile = cell.GeoTile,
                X = cell.X,
                Y = cell.Y,
                Zoom = cell.Zoom,
                Taxa = cell.Taxa.Select(m => new GeoGridTileTaxonObservationCountDto
                {
                    TaxonId = m.TaxonId,
                    ObservationCount = m.ObservationCount
                })
            };
        }

        public static GeoGridResultDto ToGeoGridResultDto(this GeoGridTileResult geoGridTileResult)
        {
            return new GeoGridResultDto
            {
                Zoom = geoGridTileResult.Zoom,
                GridCellCount = geoGridTileResult.GridCellTileCount,
                BoundingBox = geoGridTileResult.BoundingBox.ToLatLonBoundingBoxDto(),
                GridCells = geoGridTileResult.GridCellTiles.Select(cell => cell.ToGeoGridCellDto())
            };
        }

        /// <summary>
        /// Cast lat lon bounding box dto 
        /// </summary>
        /// <param name="latLonBoundingBox"></param>
        /// <returns></returns>
        public static LatLonBoundingBox ToLatLonBoundingBox(this LatLonBoundingBoxDto latLonBoundingBox)
        {
            if (latLonBoundingBox?.TopLeft == null || latLonBoundingBox?.BottomRight == null)
            {
                return null;
            }

            return new LatLonBoundingBox
            {
                TopLeft = latLonBoundingBox.TopLeft.ToLatLonCoordinate(),
                BottomRight = latLonBoundingBox.BottomRight.ToLatLonCoordinate()
            };
        }

        /// <summary>
        /// Cast dto Coordinate
        /// </summary>
        /// <param name="latLonCoordinate"></param>
        /// <returns></returns>
        public static LatLonCoordinate ToLatLonCoordinate(this LatLonCoordinateDto latLonCoordinate)
        {
            if (latLonCoordinate == null)
            {
                return null;
            } 

            return new LatLonCoordinate(latLonCoordinate.Latitude, latLonCoordinate.Longitude);
        }

        public static LatLonBoundingBoxDto ToLatLonBoundingBoxDto(this LatLonBoundingBox latLonBoundingBox)
        {
            return new LatLonBoundingBoxDto
            {
                TopLeft = latLonBoundingBox.TopLeft.ToLatLonCoordinateDto(),
                BottomRight = latLonBoundingBox.BottomRight.ToLatLonCoordinateDto()
            };
        }

        public static LatLonCoordinateDto ToLatLonCoordinateDto(this LatLonCoordinate latLonCoordinate)
        {
            return new LatLonCoordinateDto
            {
                Latitude = latLonCoordinate.Latitude,
                Longitude = latLonCoordinate.Longitude
            };
        }

        public static GeoGridCellDto ToGeoGridCellDto(this GridCellTile gridCellTile)
        {
            return new GeoGridCellDto
            {
                X = gridCellTile.X,
                Y = gridCellTile.Y,
                TaxaCount = gridCellTile.TaxaCount,
                ObservationsCount = gridCellTile.ObservationsCount,
                Zoom = gridCellTile.Zoom,
                BoundingBox = gridCellTile.BoundingBox.ToLatLonBoundingBoxDto()
            };
        }

        public static IEnumerable<TaxonAggregationItemDto> ToTaxonAggregationItemDtos(this IEnumerable<TaxonAggregationItem> taxonAggregationItems)
        {
            return taxonAggregationItems.Select(item => item.ToTaxonAggregationItemDto());
        }

        public static TaxonAggregationItemDto ToTaxonAggregationItemDto(this TaxonAggregationItem taxonAggregationItem)
        {
            return new TaxonAggregationItemDto
            {
                TaxonId = taxonAggregationItem.TaxonId,
                ObservationCount = taxonAggregationItem.ObservationCount
            };
        }

        public static PagedResultDto<TRecordDto> ToPagedResultDto<TRecord, TRecordDto>(
            this PagedResult<TRecord> pagedResult, 
            IEnumerable<TRecordDto> records)
        {
            return new PagedResultDto<TRecordDto>
            {
                Records = records,
                Skip = pagedResult.Skip,
                Take = pagedResult.Take,
                TotalCount = pagedResult.TotalCount,
            };
        }

        public static GeoPagedResultDto<TRecordDto> ToGeoPagedResultDto<TRecord, TRecordDto>(
            this PagedResult<TRecord> pagedResult,
            IEnumerable<TRecordDto> records,
            OutputFormatDto outputFormat = OutputFormatDto.Json)
        {
            if (outputFormat == OutputFormatDto.Json)
            {
                return new GeoPagedResultDto<TRecordDto>
                {
                    Records = records,
                    Skip = pagedResult.Skip,
                    Take = pagedResult.Take,
                    TotalCount = pagedResult.TotalCount,
                };
            }

            var dictionaryRecords = records.Cast<IDictionary<string, object>>();
            bool flattenProperties = outputFormat == OutputFormatDto.GeoJsonFlat;
            string geoJson = GeoJsonHelper.GetFeatureCollectionString(dictionaryRecords, flattenProperties);
            return new GeoPagedResultDto<TRecordDto>
            {
                Skip = pagedResult.Skip,
                Take = pagedResult.Take,
                TotalCount = pagedResult.TotalCount,
                GeoJson = geoJson
            };
        }

        public static ScrollResultDto<TRecordDto> ToScrollResultDto<TRecord, TRecordDto>(this ScrollResult<TRecord> scrollResult, IEnumerable<TRecordDto> records)
        {
            return new ScrollResultDto<TRecordDto>
            {
                Records = records,
                ScrollId = scrollResult.ScrollId,
                HasMorePages = scrollResult.ScrollId != null,
                Take = scrollResult.Take,
                TotalCount = scrollResult.TotalCount
            };
        }

        public static SearchFilter ToSearchFilter(this SearchFilterBaseDto searchFilterDto, string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilter)PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilter ToSearchFilter(this SearchFilterDto searchFilterDto, string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilter) PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilterInternal ToSearchFilterInternal(this SearchFilterInternalBaseDto searchFilterDto,
            string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilterInternal)PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilterInternal ToSearchFilterInternal(this SearchFilterInternalDto searchFilterDto,
            string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilterInternal)PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilter ToSearchFilter(this SearchFilterAggregationDto searchFilterDto, string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilter)PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilterInternal ToSearchFilterInternal(this SearchFilterAggregationInternalDto searchFilterDto, string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilterInternal)PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilter ToSearchFilter(this ExportFilterDto searchFilterDto, string translationCultureCode, bool protectedObservations)
        {
            return (SearchFilter)PopulateFilter(searchFilterDto, translationCultureCode, protectedObservations);
        }

        public static SearchFilterInternal ToSearchFilterInternal(this SignalFilterDto searchFilterDto)
        {
            if (searchFilterDto == null)
            {
                return null;
            }

            var searchFilter = new SearchFilterInternal
            {
                BirdNestActivityLimit = searchFilterDto.BirdNestActivityLimit,
                DataProviderIds = searchFilterDto.DataProvider?.Ids,
                Location = new LocationFilter
                {
                    Areas = searchFilterDto.Geographics?.Areas?.Select(a => new AreaFilter { FeatureId = a.FeatureId, AreaType = (AreaType)a.AreaType }),
                    Geometries = searchFilterDto.Geographics == null
                        ? null
                        : new GeographicsFilter
                        {
                            BoundingBox = searchFilterDto.Geographics.BoundingBox?.ToLatLonBoundingBox(),
                            Geometries = searchFilterDto.Geographics.Geometries?.ToList(),
                            MaxDistanceFromPoint = searchFilterDto.Geographics.MaxDistanceFromPoint,
                            UseDisturbanceRadius = searchFilterDto.Geographics.ConsiderDisturbanceRadius,
                            UsePointAccuracy = searchFilterDto.Geographics.ConsiderObservationAccuracy,
                        },
                    MaxAccuracy = searchFilterDto.Geographics?.MaxAccuracy
                },
                NotPresentFilter = SightingNotPresentFilter.DontIncludeNotPresent,
                NotRecoveredFilter = SightingNotRecoveredFilter.DontIncludeNotRecovered,
                PositiveSightings = true,
                StartDate = searchFilterDto.StartDate,
                Taxa = PopulateTaxa(searchFilterDto.Taxon),
                UnspontaneousFilter = SightingUnspontaneousFilter.NotUnspontaneous
            };
            searchFilter.ExtendedAuthorization.ProtectedObservations = true;

            return searchFilter;
        }

        public static IEnumerable<ProjectDto> ToProjectDtos(this IEnumerable<ProjectInfo> projectInfos)
        {
            return projectInfos.Select(vocabulary => vocabulary.ToProjectDto());
        }

        public static ProjectDto ToProjectDto(this ProjectInfo projectInfo)
        {
            return new ProjectDto
            {
                Id = projectInfo.Id,
                Name = projectInfo.Name,
                StartDate = projectInfo.StartDate,
                EndDate = projectInfo.EndDate,
                Category = projectInfo.Category,
                CategorySwedish = projectInfo.CategorySwedish,
                Description = projectInfo.Description,
                IsPublic = projectInfo.IsPublic,
                Owner = projectInfo.Owner,
                ProjectURL = projectInfo.ProjectURL,
                SurveyMethod = projectInfo.SurveyMethod,
                SurveyMethodUrl = projectInfo.SurveyMethodUrl
            };
        }

        public static IEnumerable<VocabularyDto> ToVocabularyDtos(this IEnumerable<Vocabulary> vocabularies, bool includeSystemMappings = true)
        {
            return vocabularies.Select(vocabulary => vocabulary.ToVocabularyDto(includeSystemMappings));
        }

        public static VocabularyDto ToVocabularyDto(this Vocabulary vocabulary, bool includeSystemMappings = true)
        {
            return new VocabularyDto
            {
                Id = (int)(VocabularyIdDto) vocabulary.Id,
                EnumId = (VocabularyIdDto)vocabulary.Id,
                Name = vocabulary.Name,
                Description = vocabulary.Description,
                Localized = vocabulary.Localized,
                Values = vocabulary.Values.Select(val => val.ToVocabularyValueInfoDto()).ToList(),
                ExternalSystemsMapping = includeSystemMappings == false || vocabulary.ExternalSystemsMapping == null ? 
                    null : 
                    vocabulary.ExternalSystemsMapping.Select(m => m.ToExternalSystemMappingDto()).ToList()
            };
        }

        private static VocabularyValueInfoDto ToVocabularyValueInfoDto(this VocabularyValueInfo vocabularyValue)
        {
            return new VocabularyValueInfoDto
            {
                Id = vocabularyValue.Id,
                Value = vocabularyValue.Value,
                Description = vocabularyValue.Description,
                Localized = vocabularyValue.Localized,
                Category = vocabularyValue.Category == null
                    ? null
                    : new VocabularyValueInfoCategoryDto
                    {
                        Id = vocabularyValue.Category.Id,
                        Name = vocabularyValue.Category.Name,
                        Description = vocabularyValue.Category.Description,
                        Localized = vocabularyValue.Category.Localized,
                        Translations = vocabularyValue.Category.Translations?.Select(
                            vocabularyValueCategoryTranslation => new VocabularyValueTranslationDto
                            {
                                CultureCode = vocabularyValueCategoryTranslation.CultureCode,
                                Value = vocabularyValueCategoryTranslation.Value
                            }).ToList()
                    },
                Translations = vocabularyValue.Translations?.Select(vocabularyValueTranslation =>
                    new VocabularyValueTranslationDto
                    {
                        CultureCode = vocabularyValueTranslation.CultureCode,
                        Value = vocabularyValueTranslation.Value
                    }).ToList()
            };
        }

        private static ExternalSystemMappingDto ToExternalSystemMappingDto(
            this ExternalSystemMapping vocabularyExternalSystemsMapping)
        {
            return new ExternalSystemMappingDto
            {
                Id = (ExternalSystemIdDto) vocabularyExternalSystemsMapping.Id,
                Name = vocabularyExternalSystemsMapping.Name,
                Description = vocabularyExternalSystemsMapping.Description,
                Mappings = vocabularyExternalSystemsMapping.Mappings?.Select(vocabularyExternalSystemsMappingMapping =>
                    new ExternalSystemMappingFieldDto
                    {
                        Key = vocabularyExternalSystemsMappingMapping.Key,
                        Description = vocabularyExternalSystemsMappingMapping.Description,
                        Values = vocabularyExternalSystemsMappingMapping.Values?.Select(
                            vocabularyExternalSystemsMappingMappingValue => new ExternalSystemMappingValueDto
                            {
                                Value = vocabularyExternalSystemsMappingMappingValue.Value,
                                SosId = vocabularyExternalSystemsMappingMappingValue.SosId
                            }).ToList()
                    }).ToList()
            };
        }

        public static TaxonListTaxonInformationDto ToTaxonListTaxonInformationDto(
            this TaxonListTaxonInformation taxonInformation)
        {
            return new TaxonListTaxonInformationDto
            {
                Id = taxonInformation.Id,
                ScientificName = taxonInformation.ScientificName,
                SwedishName = taxonInformation.SwedishName
            };
        }

        /// <summary>
        /// Cast Location to locationDto
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static LocationDto ToDto(this Location location)
        {
            if (location == null)
            {
                return null;
            }

            return new LocationDto
            {
                Continent = location.Continent == null
                    ? null
                    : new IdValueDto<int> {Id = location.Continent.Id, Value = location.Continent.Value},
                CoordinatePrecision = location.CoordinatePrecision,
                CoordinateUncertaintyInMeters = location.CoordinateUncertaintyInMeters,
                Country = location.Country == null
                    ? null
                    : new IdValueDto<int> { Id = location.Country.Id, Value = location.Country.Value },
                CountryCode = location.CountryCode,
                County = location.County == null
                    ? null
                    : new IdValueDto<string> { Id= location.County.FeatureId, Value = location.County.Name },
                DecimalLatitude = location.DecimalLatitude,
                DecimalLongitude = location.DecimalLongitude,
                Locality = location.Locality,
                LocationAccordingTo = location.LocationAccordingTo,
                LocationId = location.LocationId,
                LocationRemarks = location.LocationRemarks,
                Municipality = location.Municipality == null
                    ? null
                    : new IdValueDto<string> { Id = location.Municipality.FeatureId, Value = location.Municipality.Name },
                Province = location.Province == null
                    ? null
                    : new IdValueDto<string> { Id = location.Province.FeatureId, Value = location.Province.Name },
                Parish = location.Parish == null
                    ? null
                    : new IdValueDto<string> { Id = location.Parish.FeatureId, Value = location.Parish.Name },
                Point = location.Point,
                PointWithBuffer = location.PointWithBuffer,
                PointWithDisturbanceBuffer = location.PointWithDisturbanceBuffer
            };
        }

        public static List<PropertyFieldDescriptionDto> ToPropertyFieldDescriptionDtos(this IEnumerable<PropertyFieldDescription> fieldDescriptions)
        {
            return fieldDescriptions.Select(fieldDescription => fieldDescription.ToPropertyFieldDescriptionDto()).ToList();
        }

        public static PropertyFieldDescriptionDto ToPropertyFieldDescriptionDto(this PropertyFieldDescription fieldDescription)
        {
            if (fieldDescription == null)
            {
                return null;
            }

            return new PropertyFieldDescriptionDto
            {
                PropertyPath = fieldDescription.PropertyPath,
                DataType = fieldDescription.DataTypeEnum,
                DataTypeNullable = fieldDescription.DataTypeNullable.GetValueOrDefault(false),
                DwcIdentifier = fieldDescription.DwcIdentifier,
                DwcName = fieldDescription.DwcName,
                EnglishTitle = fieldDescription.GetEnglishTitle(),
                SwedishTitle = fieldDescription.GetSwedishTitle(),
                Name = fieldDescription.Name,
                FieldSet = fieldDescription.FieldSetEnum,
                PartOfFieldSets = fieldDescription.FieldSets
            };
        }
    }
}