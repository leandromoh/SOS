﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SOS.Lib.Models.Gis;
using SOS.Lib.Models.Search;
using SOS.Observations.Api.Dtos;
using SOS.Observations.Api.Dtos.Filter;

namespace SOS.Observations.Api.Extensions
{
    public static class DtoExtensions
    {
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

        public static PagedResultDto<TRecordDto> ToPagedResultDto<TRecord, TRecordDto>(this PagedResult<TRecord> pagedResult, IEnumerable<TRecordDto> records)
        {
            return new PagedResultDto<TRecordDto>
            {
                Records = records,
                Skip = pagedResult.Skip,
                Take = pagedResult.Take,
                TotalCount = pagedResult.TotalCount
            };
        }

        public static SearchFilter ToSearchFilter(this SearchFilterDto searchFilterDto)
        {
            if (searchFilterDto == null) return null;

            var filter = new SearchFilter();
            filter.OutputFields = searchFilterDto.OutputFields;
            filter.StartDate = searchFilterDto.Date?.StartDate;
            filter.EndDate = searchFilterDto.Date?.EndDate;
            filter.SearchOnlyBetweenDates = (searchFilterDto.Date?.SearchOnlyBetweenDates).GetValueOrDefault();
            filter.AreaIds = searchFilterDto.AreaIds;
            filter.TaxonIds = searchFilterDto.Taxon?.TaxonIds;
            filter.IncludeUnderlyingTaxa = (searchFilterDto.Taxon?.IncludeUnderlyingTaxa).GetValueOrDefault();
            filter.RedListCategories = searchFilterDto.Taxon?.RedListCategories;
            filter.DataProviderIds = searchFilterDto.DataProviderIds;
            filter.FieldTranslationCultureCode = searchFilterDto.TranslationCultureCode;
            filter.OnlyValidated = searchFilterDto.OnlyValidated;
            filter.GeometryFilter = searchFilterDto.Geometry == null ? null : new GeometryFilter
            {
                Geometries = searchFilterDto.Geometry.Geometries,
                MaxDistanceFromPoint = searchFilterDto.Geometry.MaxDistanceFromPoint,
                UsePointAccuracy = searchFilterDto.Geometry.UsePointAccuracy
            };

            if (searchFilterDto.OccurrenceStatus != null)
            {
                switch (searchFilterDto.OccurrenceStatus)
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

            return filter;
        }
    }
}