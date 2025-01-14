﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using SOS.Lib.Enums;
using SOS.Lib.Enums.VocabularyValues;
using SOS.Lib.Extensions;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.Models.Cache;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Verbatim.Artportalen;
using SOS.Lib.Repositories.Resource.Interfaces;

namespace SOS.Lib.Helpers
{
    public class AreaHelper : IAreaHelper
    {
        private readonly AreaType[] _areaTypes =
            {AreaType.County, AreaType.Province, AreaType.Municipality, AreaType.Parish, AreaType.EconomicZoneOfSweden};

        private IDictionary<string, PositionLocation> _featureCache;
        private readonly IAreaRepository _processedAreaRepository;
        private STRtree<IFeature> _strTree;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="processedAreaRepository"></param>
        public AreaHelper(
            IAreaRepository processedAreaRepository)
        {
            _processedAreaRepository = processedAreaRepository ??
                                       throw new ArgumentNullException(nameof(processedAreaRepository));

            ClearCache();
        }


        /// <inheritdoc />
        public void AddAreaDataToProcessedObservations(IEnumerable<Observation> processedObservations)
        {
            foreach (var processedObservation in processedObservations)
            {
                AddAreaDataToProcessedObservation(processedObservation);
            }
        }

        /// <inheritdoc />
        public void AddAreaDataToProcessedObservation(Observation processedObservation)
        {
            if ((processedObservation?.Location?.DecimalLongitude ?? 0) == 0 
                || (processedObservation?.Location?.DecimalLatitude ?? 0) == 0)
            {
                return;
            }

            var positionLocation = GetPositionLocation(processedObservation.Location.DecimalLongitude.Value,
                processedObservation.Location.DecimalLatitude.Value);
            processedObservation.Location.County = positionLocation?.County;
            processedObservation.Location.Municipality = positionLocation?.Municipality;
            processedObservation.Location.Parish = positionLocation?.Parish;
            processedObservation.Location.Province = positionLocation?.Province;
            processedObservation.IsInEconomicZoneOfSweden = positionLocation?.EconomicZoneOfSweden ?? false;

            processedObservation.Location.Attributes.ProvincePartIdByCoordinate =
                GetProvincePartIdByCoordinate(processedObservation.Location.Province?.FeatureId);

            processedObservation.Location.Attributes.CountyPartIdByCoordinate = GetCountyPartIdByCoordinate(
                processedObservation.Location.County?.FeatureId, processedObservation.Location.Province?.FeatureId);
        }

        /// <inheritdoc />
        public void AddAreaDataToSite(Site site)
        {
            if (site.Point == null)
            {
                return;
            }

         /*   var coordinates = site.Point.Coordinates.ToArray().Select(p => (double)p).ToArray();
            var positionLocation = GetPositionLocation(coordinates[0], coordinates[1]);
            site.County = positionLocation?.County == null ? null :new GeographicalArea
            {
                FeatureId = positionLocation.County.FeatureId,
                Name = positionLocation.County.Name
            };
            site.Municipality = positionLocation?.Municipality == null ? null : new GeographicalArea
            {
                FeatureId = positionLocation.Municipality.FeatureId,
                Name = positionLocation.Municipality.Name
            };
            site.Parish = positionLocation?.Parish == null ? null : new GeographicalArea
            {
                FeatureId = positionLocation.Parish.FeatureId,
                Name = positionLocation.Parish.Name
            };
            site.Province = positionLocation?.Province == null ? null : new GeographicalArea
            {
                FeatureId = positionLocation.Province.FeatureId,
                Name = positionLocation.Province.Name
            };*/
            site.ProvincePartIdByCoordinate =
                GetProvincePartIdByCoordinate(site.Province?.FeatureId);

            site.CountyPartIdByCoordinate = GetCountyPartIdByCoordinate(
               site.County?.FeatureId, site.Province?.FeatureId);
        }

        public void ClearCache()
        {
            IsInitialized = false;
            _featureCache = new ConcurrentDictionary<string, PositionLocation>();
            _strTree = new STRtree<IFeature>();
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // If tree already initialized, return
            if (IsInitialized)
            {
                return;
            }
            
            ClearCache();

            var areas = await _processedAreaRepository.GetAsync(_areaTypes);
            foreach (var area in areas)
            {
                var geometry = await _processedAreaRepository.GetGeometryAsync(area.AreaType, area.FeatureId);

                var attributes = new Dictionary<string, object>();
                attributes.Add("name", area.Name);
                attributes.Add("areaType", area.AreaType);
                attributes.Add("featureId", area.FeatureId);

                var feature = geometry.ToFeature(attributes);
                _strTree.Insert(feature.Geometry.EnvelopeInternal, feature);
            }

            _strTree.Build();
            IsInitialized = true;
        }

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     Get all features where position is inside area
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        private IEnumerable<IFeature> GetPointFeatures(double longitude, double latitude)
        {
            var factory = new GeometryFactory();
            var point = factory.CreatePoint(new Coordinate(longitude, latitude));

            var featuresContainingPoint = new List<IFeature>();
            var possibleFeatures = _strTree.Query(point.EnvelopeInternal);
            foreach (var feature in possibleFeatures)
            {
                if (feature.Geometry.Contains(point))
                {
                    featuresContainingPoint.Add(feature);
                }
            }

            return featuresContainingPoint;
        }

        private PositionLocation GetPositionLocation(double decimalLongitude, double decimalLatitude)
        {
            // Round coordinates to 5 decimals (roughly 1m)
            var key = $"{Math.Round(decimalLongitude, 5)}-{Math.Round(decimalLatitude, 5)}";

            // Try to get areas from cache. If areas not found for that position, try to get from repository
            if (!_featureCache.TryGetValue(key, out var positionLocation))
            {
                var features = GetPointFeatures(decimalLongitude, decimalLatitude);
                positionLocation = new PositionLocation();

                if (features != null)
                {
                    foreach (var feature in features)
                    {
                        if (Enum.TryParse(typeof(AreaType), feature.Attributes.GetOptionalValue("areaType").ToString(),
                            out var areaType))
                        {
                            var area = new Area
                            {
                                FeatureId = feature.Attributes.GetOptionalValue("featureId")?.ToString(),
                                Name = feature.Attributes.GetOptionalValue("name")?.ToString()
                            };
                            switch ((AreaType)areaType)
                            {
                                case AreaType.County:
                                    positionLocation.County = area;
                                    break;
                                case AreaType.Municipality:
                                    positionLocation.Municipality = area;
                                    break;
                                case AreaType.Parish:
                                    positionLocation.Parish = area;
                                    break;
                                case AreaType.Province:
                                    positionLocation.Province = area;
                                    break;
                                case AreaType.EconomicZoneOfSweden:
                                    positionLocation.EconomicZoneOfSweden = true;
                                    break;
                            }
                        }
                    }
                }

                if (!_featureCache.ContainsKey(key))
                {
                    _featureCache.TryAdd(key, positionLocation);
                }
            }

            return positionLocation;
        }

        private static void SetProvincePartIdByCoordinate(Observation processedObservation)
        {
            // Set ProvincePartIdByCoordinate. Merge lappmarker into Lappland.
            processedObservation.Location.Attributes.ProvincePartIdByCoordinate = processedObservation.Location.Province?.FeatureId;
            if (new[]
            {
                ProvinceIds.LuleLappmark,
                ProvinceIds.LyckseleLappmark,
                ProvinceIds.PiteLappmark,
                ProvinceIds.TorneLappmark,
                ProvinceIds.ÅseleLappmark
            }.Contains(processedObservation.Location.Province?.FeatureId))
            {
                processedObservation.Location.Attributes.ProvincePartIdByCoordinate = SpecialProvincePartId.Lappland;
            }
        }

        private static void SetCountyPartIdByCoordinate(Observation processedObservation)
        {
            // Set CountyPartIdByCoordinate. Split Kalmar into Öland and Kalmar fastland.
            processedObservation.Location.Attributes.CountyPartIdByCoordinate = processedObservation.Location.County?.FeatureId;
            if (processedObservation.Location.County?.FeatureId == CountyId.Kalmar)
            {
                if (processedObservation.Location.Province?.FeatureId == ProvinceIds.Öland)
                {
                    processedObservation.Location.Attributes.CountyPartIdByCoordinate = SpecialCountyPartId.Öland;
                }
                else
                {
                    processedObservation.Location.Attributes.CountyPartIdByCoordinate = SpecialCountyPartId.KalmarFastland;
                }
            }
        }

        private static string GetProvincePartIdByCoordinate(string provinceFeatureId)
        {
            if (new[]
            {
                ProvinceIds.LuleLappmark,
                ProvinceIds.LyckseleLappmark,
                ProvinceIds.PiteLappmark,
                ProvinceIds.TorneLappmark,
                ProvinceIds.ÅseleLappmark
            }.Contains(provinceFeatureId))
            {
               return SpecialProvincePartId.Lappland;
            }

            return provinceFeatureId;
        }

        private static string GetCountyPartIdByCoordinate(string countyFeatureId, string provinceFeatureId)
        {
            if (countyFeatureId == CountyId.Kalmar)
            {
                if (provinceFeatureId == ProvinceIds.Öland)
                {
                    return SpecialCountyPartId.Öland;
                }
                else
                {
                    return SpecialCountyPartId.KalmarFastland;
                }
            }

            return countyFeatureId;
        }
    }
}