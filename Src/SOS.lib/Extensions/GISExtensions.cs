﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Types;
using Nest;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Utilities;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;

namespace SOS.Lib.Extensions
{
    /// <summary>
    ///     NetTopologySuite extensions
    /// </summary>
    public static class GISExtensions
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        static GISExtensions()
        {
            var coordinateSystemFactory = new CoordinateSystemFactory();
            var coordinateSystemsById = new Dictionary<CoordinateSys, CoordinateSystem>();

            foreach (var coordinateSys in Enum.GetValues(typeof(CoordinateSys)).Cast<CoordinateSys>())
            {
                coordinateSystemsById.Add(coordinateSys,
                    coordinateSystemFactory.CreateFromWkt(GetCoordinateSystemWkt(coordinateSys)));
            }

            var coordinateTransformationFactory = new CoordinateTransformationFactory();
            foreach (var sourceCoordinateSys in Enum.GetValues(typeof(CoordinateSys)).Cast<CoordinateSys>())
            {
                foreach (var targetCoordinateSys in Enum.GetValues(typeof(CoordinateSys)).Cast<CoordinateSys>())
                {
                    var coordinateTransformation = coordinateTransformationFactory.CreateFromCoordinateSystems(
                        coordinateSystemsById[sourceCoordinateSys],
                        coordinateSystemsById[targetCoordinateSys]);

                    MathTransformFilterDictionary.Add(
                        new Tuple<CoordinateSys, CoordinateSys>(sourceCoordinateSys, targetCoordinateSys),
                        new MathTransformFilter(coordinateTransformation.MathTransform));
                }
            }
        }

        #region Private

        private static readonly ConcurrentDictionary<long, ICoordinateTransformation> TransformationsDictionary =
            new ConcurrentDictionary<long, ICoordinateTransformation>();

        private static readonly Dictionary<Tuple<CoordinateSys, CoordinateSys>, MathTransformFilter>
            MathTransformFilterDictionary = new Dictionary<Tuple<CoordinateSys, CoordinateSys>, MathTransformFilter>();

        /// <summary>
        ///     Math transform filter
        /// </summary>
        private sealed class MathTransformFilter : ICoordinateSequenceFilter
        {
            private readonly MathTransform _mathTransform;

            public MathTransformFilter(MathTransform mathTransform)
            {
                _mathTransform = mathTransform;
            }

            public bool Done => false;
            public bool GeometryChanged => true;

            public void Filter(CoordinateSequence seq, int i)
            {
                var (x, y, z) = _mathTransform.Transform(seq.GetX(i), seq.GetY(i), seq.GetZ(i));
                seq.SetX(i, x);
                seq.SetY(i, y);
                seq.SetZ(i, z);
            }
        }

        /// <summary>
        ///     Get coordinate system wkt
        /// </summary>
        /// <param name="coordinateSystem"></param>
        /// <returns></returns>
        private static string GetCoordinateSystemWkt(CoordinateSys coordinateSystem)
        {
            switch (coordinateSystem)
            {
                case CoordinateSys.WebMercator:
                    return
                        @"PROJCS[""Google Mercator"",GEOGCS[""WGS 84"",DATUM[""World Geodetic System 1984"",SPHEROID[""WGS 84"", 6378137.0, 298.257223563, AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"", 0.0, AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"", 0.017453292519943295], AXIS[""Geodetic latitude"", NORTH], AXIS[""Geodetic longitude"", EAST], AUTHORITY[""EPSG"",""4326""]], PROJECTION[""Mercator_1SP""], PARAMETER[""semi_minor"", 6378137.0], PARAMETER[""latitude_of_origin"", 0.0], PARAMETER[""central_meridian"", 0.0], PARAMETER[""scale_factor"", 1.0], PARAMETER[""false_easting"", 0.0], PARAMETER[""false_northing"", 0.0], UNIT[""m"", 1.0], AXIS[""Easting"", EAST], AXIS[""Northing"", NORTH], AUTHORITY[""EPSG"",""900913""]]";
                //return @"PROJCS[""WGS 84 / Pseudo - Mercator"",GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]],PROJECTION[""Mercator_1SP""],PARAMETER[""central_meridian"",0],PARAMETER[""scale_factor"",1],PARAMETER[""false_easting"",0],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""X"",EAST],AXIS[""Y"",NORTH],EXTENSION[""PROJ4"","" + proj = merc + a = 6378137 + b = 6378137 + lat_ts = 0.0 + lon_0 = 0.0 + x_0 = 0.0 + y_0 = 0 + k = 1.0 + units = m + nadgrids = @null + wktext + no_defs""],AUTHORITY[""EPSG"",""3857""]]";
                case CoordinateSys.Rt90_25_gon_v:
                    return
                        @"PROJCS[""SWEREF99 / RT90 2.5 gon V emulation"", GEOGCS[""SWEREF99"", DATUM[""SWEREF99"", SPHEROID[""GRS 1980"",6378137.0,298.257222101, AUTHORITY[""EPSG"",""7019""]], TOWGS84[0.0,0.0,0.0,0.0,0.0,0.0,0.0], AUTHORITY[""EPSG"",""6619""]], PRIMEM[""Greenwich"",0.0, AUTHORITY[""EPSG"",""8901""]], UNIT[""degree"",0.017453292519943295], AXIS[""Geodetic latitude"",NORTH], AXIS[""Geodetic longitude"",EAST], AUTHORITY[""EPSG"",""4619""]], PROJECTION[""Transverse Mercator""], PARAMETER[""central_meridian"",15.806284529444449], PARAMETER[""latitude_of_origin"",0.0], PARAMETER[""scale_factor"",1.00000561024], PARAMETER[""false_easting"",1500064.274], PARAMETER[""false_northing"",-667.711], UNIT[""m"",1.0], AXIS[""Northing"",NORTH], AXIS[""Easting"",EAST], AUTHORITY[""EPSG"",""3847""]]";
                case CoordinateSys.SWEREF99:
                    return
                        @"GEOGCS[""SWEREF99"", DATUM[""SWEREF99"", SPHEROID[""GRS 1980"", 6378137, 298.257222101, AUTHORITY[""EPSG"", ""7019""]], TOWGS84[0, 0, 0, 0, 0, 0, 0], AUTHORITY[""EPSG"", ""6619""]], PRIMEM[""Greenwich"", 0, AUTHORITY[""EPSG"", ""8901""]], UNIT[""degree"", 0.0174532925199433, AUTHORITY[""EPSG"", ""9122""]], AUTHORITY[""EPSG"", ""4619""]]";
                case CoordinateSys.SWEREF99_TM:
                    return
                        @"PROJCS[""SWEREF99 TM"", GEOGCS[""SWEREF99"", DATUM[""D_SWEREF99"", SPHEROID[""GRS_1980"",6378137,298.257222101]], PRIMEM[""Greenwich"",0], UNIT[""Degree"",0.017453292519943295]], PROJECTION[""Transverse_Mercator""], PARAMETER[""latitude_of_origin"",0], PARAMETER[""central_meridian"",15], PARAMETER[""scale_factor"",0.9996], PARAMETER[""false_easting"",500000], PARAMETER[""false_northing"",0], UNIT[""Meter"",1]]";
                case CoordinateSys.WGS84:
                    return
                        @"GEOGCS[""GCS_WGS_1984"", DATUM[""WGS_1984"", SPHEROID[""WGS_1984"",6378137,298.257223563]], PRIMEM[""Greenwich"",0], UNIT[""Degree"",0.017453292519943295]]";
                case CoordinateSys.ETRS89:
                    return
                        @"PROJCS[""ETRS89 / LAEA Europe"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],PROJECTION[""Lambert_Azimuthal_Equal_Area""],PARAMETER[""latitude_of_center"",52],PARAMETER[""longitude_of_center"",10],PARAMETER[""false_easting"",4321000],PARAMETER[""false_northing"",3210000],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AUTHORITY[""EPSG"",""3035""]]";
                default:
                    throw new ArgumentException("Not handled coordinate system id " + coordinateSystem);
            }
        }

        /// <summary>
        ///     Cast polygon to shape coordinates
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static IEnumerable<IEnumerable<GeoCoordinate>> ToGeoShapePolygonCoordinates(this Polygon polygon)
        {
            var coordinates = new List<IEnumerable<GeoCoordinate>>();
            var exteriorRing = polygon.ExteriorRing.Coordinates.Select(p => new GeoCoordinate(p.Y, p.X));
            var holes = polygon.Holes.Select(h => h.Coordinates.Select(p => new GeoCoordinate(p.Y, p.X)));

            coordinates.Add(exteriorRing);
            coordinates.AddRange(holes);

            return coordinates;
        }

        /// <summary>
        ///     Cast geo josn polygon coordinates to geo shape polygon coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static IEnumerable<IEnumerable<GeoCoordinate>> ToGeoShapePolygonCoordinates(this ArrayList coordinates)
        {
            var rings = coordinates.ToArray()
                .Select(lr => (lr as IEnumerable<double[]>).Select(c => new GeoCoordinate(c[1], c[0])));
            var exteriorRing = rings.First();
            var holes = rings.Skip(1);

            var newCoordinates = new List<IEnumerable<GeoCoordinate>>();

            newCoordinates.Add(exteriorRing);
            newCoordinates.AddRange(holes);

            return newCoordinates;
        }

        /// <summary>
        ///     Cast polygon coordinates to geo json polygon coordinates
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static ArrayList ToGeoJsonPolygonCoordinates(this Polygon polygon)
        {
            var coordinates = new List<IEnumerable<IEnumerable<double>>>();
            var exteriorRing = polygon.ExteriorRing.Coordinates.Select(p => new[] {p.X, p.Y}).ToArray();
            var holes = polygon.Holes.Select(h => h.Coordinates.Select(p => new[] {p.X, p.Y})).ToArray();

            coordinates.Add(exteriorRing);
            coordinates.AddRange(holes);

            return new ArrayList(coordinates.ToArray());
        }

        /// <summary>
        ///     Convert angle to radians
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static double ToRadians(this double val)
        {
            return Math.PI / 180 * val;
        }

        /// <summary>
        ///     Make polygon valid
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static Polygon MakeValid(this Polygon polygon)
        {
            if (polygon.IsValid)
            {
                return polygon;
            }

            return (Polygon) polygon.Buffer(0);
        }

        #endregion Private

        #region Public

        /// <summary>
        ///     Get the EPSG code for the specified coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <returns>The EPSG code.</returns>
        public static string EpsgCode(this CoordinateSys coordinateSystem)
        {
            return $"EPSG:{coordinateSystem.Srid()}";
        }

        /// <summary>
        ///     Gets the Srid for the specified coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <returns>The Srid.</returns>
        public static int Srid(this CoordinateSys coordinateSystem)
        {
            return (int) coordinateSystem; // the enum value is the same as SRID.
        }

        /// <summary>
        ///     Transform WGS 84 point to circle by adding a buffer to it
        /// </summary>
        /// <param name="point"></param>
        /// <param name="accuracy"></param>
        /// <param name="defaultWhenAccuracyIsUnknown"></param>
        /// <returns></returns>
        public static Geometry ToCircle(this Point point, int? accuracy, int defaultWhenAccuracyIsUnknown = 10000)
        {
            if (point?.Coordinate == null || point.Coordinate.X.Equals(0) || point.Coordinate.Y.Equals(0))
            {
                return null;
            }

            if (accuracy == null || accuracy < 0)
            {
                accuracy = defaultWhenAccuracyIsUnknown;
            }
            else if (accuracy == 0)
            {
                accuracy = 1;
            }

            Geometry circle = null;
            switch ((CoordinateSys) point.SRID)
            {
                // Metric systems, add buffer to create a circle
                case CoordinateSys.SWEREF99:
                case CoordinateSys.SWEREF99_TM:
                case CoordinateSys.Rt90_25_gon_v:
                    circle = point.Buffer((double) (accuracy == 0 ? 1 : accuracy));
                    break;
                case CoordinateSys.WebMercator:
                case CoordinateSys.WGS84:
                    // Degree systems
                    var diameterInMeters = (double) (accuracy == 0 ? 1 : accuracy * 2);

                    var shapeFactory = new GeometricShapeFactory();
                    shapeFactory.NumPoints = accuracy < 1000 ? 32 : accuracy < 10000 ? 64 : 128;
                    shapeFactory.Centre = point.Coordinate;

                    // Length in meters of 1° of latitude = always 111.32 km
                    shapeFactory.Height = diameterInMeters / 111320d;

                    // Length in meters of 1° of longitude = 40075 km * cos( latitude radian ) / 360
                    shapeFactory.Width = diameterInMeters / (40075000 * Math.Cos(point.Y.ToRadians()) / 360);

                    circle = shapeFactory.CreateCircle();
                    break;
            }

            return circle;
        }

        public static IFeature ToFeature(this IGeoShape geometry, IDictionary<string, object> attributes = null)
        {
            if (geometry == null)
            {
                return null;
            }

            Geometry featureGeometry = null;

            switch (geometry.Type?.ToLower())
            {
                case "point":
                    var point = (PointGeoShape) geometry;
                    featureGeometry = new Point(point.Coordinates.Longitude, point.Coordinates.Latitude);
                    break;
                case "polygon":
                    var polygon = (PolygonGeoShape) geometry;
                    var linearRings = polygon.Coordinates.Select(lr =>
                            new LinearRing(lr.Select(pnt => new Coordinate(pnt.Longitude, pnt.Latitude)).ToArray()))
                        .ToArray();

                    featureGeometry = new Polygon(linearRings.First(), linearRings.Skip(1)?.ToArray());
                    break;
                case "multipolygon":
                    var multiPolygons = (MultiPolygonGeoShape) geometry;
                    var polygons = new List<Polygon>();

                    foreach (var poly in multiPolygons.Coordinates)
                    {
                        var lr = poly.Select(lr =>
                                new LinearRing(lr.Select(pnt => new Coordinate(pnt.Longitude, pnt.Latitude)).ToArray()))
                            .ToArray();

                        polygons.Add(new Polygon(lr.First(), lr.Skip(1)?.ToArray()));
                    }

                    featureGeometry = new MultiPolygon(polygons.ToArray());
                    break;
                default:
                    return null;
            }

            return new Feature {Geometry = featureGeometry, Attributes = new AttributesTable(attributes)};
        }

        /// <summary>
        ///     Cast geometry to geo json
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static GeoJsonGeometry ToGeoJson(this Geometry geometry)
        {
            if (geometry?.Coordinates == null)
            {
                return null;
            }

            var coordinates = new ArrayList();
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    var point = (Point) geometry;
                    coordinates.Add(point.Coordinate.X);
                    coordinates.Add(point.Coordinate.Y);
                    break;
                case OgcGeometryType.Polygon:
                    var polygon = (Polygon) geometry;
                    coordinates = polygon.ToGeoJsonPolygonCoordinates();
                    break;
                case OgcGeometryType.MultiPolygon:
                    var multiPolygon = (MultiPolygon) geometry;

                    foreach (var geom in multiPolygon.Geometries)
                    {
                        coordinates.Add(((Polygon) geom).ToGeoJsonPolygonCoordinates());
                    }

                    break;
                default:
                    throw new ArgumentException($"Not handled geometry type: {geometry.GeometryType}");
            }

            return new GeoJsonGeometry
            {
                Type = geometry.OgcGeometryType.ToString(),
                Coordinates = coordinates
            };
        }

        /// <summary>
        ///     Cast geo shape to geo json geometry
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static GeoJsonGeometry ToGeoJson(this IGeoShape geometry)
        {
            if (geometry == null)
            {
                return null;
            }

            var coordinates = new ArrayList();
            var type = "";
            switch (geometry.Type?.ToLower())
            {
                case "point":
                    var point = (PointGeoShape) geometry;
                    coordinates.Add(point.Coordinates.Longitude);
                    coordinates.Add(point.Coordinates.Latitude);
                    type = "Point"; // Type in correct case
                    break;
                case "polygon":
                    var polygon = (PolygonGeoShape) geometry;
                    coordinates.AddRange(polygon.Coordinates
                        .Select(ls => ls.Select(pnt => new[] {pnt.Longitude, pnt.Latitude})).ToArray());
                    type = "Polygon"; // Type in correct case
                    break;
                case "multipolygon":
                    var multiPolygons = (MultiPolygonGeoShape) geometry;
                    coordinates.AddRange(multiPolygons.Coordinates
                        .Select(p => p.Select(ls => ls.Select(pnt => new[] {pnt.Longitude, pnt.Latitude}))).ToArray());
                    type = "MultiPolygon"; // Type in correct case
                    break;
                default:
                    return null;
            }

            return new GeoJsonGeometry
            {
                Type = type,
                Coordinates = coordinates
            };
        }

        /// <summary>
        ///     Cast point to geo location
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static GeoLocation ToGeoLocation(this Point point)
        {
            if (point?.Coordinate == null || point.Coordinate.X.Equals(0) || point.Coordinate.Y.Equals(0))
            {
                return null;
            }

            return new GeoLocation(point.Y, point.X);
        }

        /// <summary>
        ///     Cast GeoJsonGeometry (point) to geo location
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static GeoLocation ToGeoLocation(this GeoJsonGeometry geometry)
        {
            if (geometry?.Type?.ToLower() != "point")
            {
                return null;
            }

            return new GeoLocation((double) geometry.Coordinates[1], (double) geometry.Coordinates[0]);
        }

        /// <summary>
        ///     Cast IGeoShape (point) to geo location
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static GeoLocation ToGeoLocation(this IGeoShape geometry)
        {
            if (geometry?.Type?.ToLower() != "point")
            {
                return null;
            }

            var point = (PointGeoShape) geometry;

            return new GeoLocation(point.Coordinates.Latitude, point.Coordinates.Longitude);
        }

        /// <summary>
        ///     Cast geojson geometry to Geo shape
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static IGeoShape ToGeoShape(this GeoJsonGeometry geometry)
        {
            if (!geometry.IsValid)
            {
                return null;
            }

            switch (geometry.Type?.ToLower())
            {
                case "point":
                    var coordinates = geometry.Coordinates.ToArray().Select(p => (double) p).ToArray();
                    return new PointGeoShape(new GeoCoordinate(coordinates[1], coordinates[0]));
                case "polygon":
                case "holepolygon":
                    var polygonCoordinates = geometry.Coordinates.ToGeoShapePolygonCoordinates();
                    return new PolygonGeoShape(polygonCoordinates);
                case "multipolygon":
                    var multiPolygonCoordinates = new List<IEnumerable<IEnumerable<GeoCoordinate>>>();
                    foreach (var polyCoorinates in geometry.Coordinates)
                    {
                        multiPolygonCoordinates.Add(((ArrayList) polyCoorinates).ToGeoShapePolygonCoordinates());
                    }

                    return new MultiPolygonGeoShape(multiPolygonCoordinates);
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Cast geometry to geo shape
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static IGeoShape ToGeoShape(this Geometry geometry)
        {
            if (geometry?.Coordinates == null)
            {
                return null;
            }

            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    var point = (Point) geometry;

                    return point.X.Equals(0) || point.Y.Equals(0)
                        ? null
                        : new PointGeoShape(new GeoCoordinate(point.Y, point.X));
                case OgcGeometryType.LineString:
                    var lineString = (LineString) geometry;

                    return new LineStringGeoShape(lineString.Coordinates.Select(p => new GeoCoordinate(p.Y, p.X)));
                case OgcGeometryType.MultiLineString:
                    var multiLineString = (MultiLineString) geometry;

                    return new MultiLineStringGeoShape(multiLineString.Geometries.Select(mls =>
                        mls.Coordinates.Select(p => new GeoCoordinate(p.Y, p.X))));
                case OgcGeometryType.MultiPoint:
                    var multiPoint = (MultiPoint) geometry;

                    return new MultiPointGeoShape(multiPoint.Coordinates.Select(p => new GeoCoordinate(p.Y, p.X)));
                case OgcGeometryType.Polygon:
                    var polygon = (Polygon) geometry;

                    return new PolygonGeoShape(polygon.MakeValid().ToGeoShapePolygonCoordinates());
                case OgcGeometryType.MultiPolygon:
                    var multiPolygon = (MultiPolygon) geometry;

                    return new MultiPolygonGeoShape(multiPolygon.Geometries.Select(p =>
                        ((Polygon) p).MakeValid().ToGeoShapePolygonCoordinates()));
                default:
                    throw new ArgumentException($"Not handled geometry type: {geometry.GeometryType}");
            }
        }

        /// <summary>
        ///     Cast sql geometry to IGeometry
        /// </summary>
        /// <param name="sqlGeometry"></param>
        /// <returns></returns>
        public static Geometry ToGeometry(
            this SqlGeometry sqlGeometry)
        {
            var factory = new GeometryFactory();
            var wktReader = new WKTReader(factory);
            return wktReader.Read(sqlGeometry.STAsText().ToSqlString().ToString());
        }

        /// <summary>
        ///     Transform coordinates
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="fromCoordinateSystem"></param>
        /// <param name="toCoordinateSystem"></param>
        /// <returns></returns>
        public static Geometry Transform(
            this Geometry geometry,
            CoordinateSys fromCoordinateSystem,
            CoordinateSys toCoordinateSystem)
        {
            if (fromCoordinateSystem == toCoordinateSystem)
            {
                return geometry;
            }

            var mathTransformFilter =
                MathTransformFilterDictionary[
                    new Tuple<CoordinateSys, CoordinateSys>(fromCoordinateSystem, toCoordinateSystem)];
            var transformedGeometry = geometry.Copy();
            transformedGeometry.Apply(mathTransformFilter);
            transformedGeometry.SRID = (int) toCoordinateSystem;
            return transformedGeometry;
        }

        /// <summary>
        ///     Try parse coordinate system.
        /// </summary>
        /// <param name="strCoordinateSystem"></param>
        /// <param name="coordinateSystem"></param>
        /// <returns></returns>
        public static bool TryParseCoordinateSystem(string strCoordinateSystem, out CoordinateSys coordinateSystem)
        {
            strCoordinateSystem = strCoordinateSystem.Trim();

            if (strCoordinateSystem.Equals("epsg:3006", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.SWEREF99_TM;
                return true;
            }

            if (strCoordinateSystem.Equals("epsg:3021", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.Rt90_25_gon_v;
                return true;
            }

            if (strCoordinateSystem.Equals("epsg:3857", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.WebMercator;
                return true;
            }

            if (strCoordinateSystem.Equals("epsg:900913", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.WebMercator;
                return true;
            }

            if (strCoordinateSystem.Equals("epsg:4326", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.WGS84;
                return true;
            }

            if (strCoordinateSystem.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.WGS84;
                return true;
            }

            if (strCoordinateSystem.Equals("wgs 84", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.WGS84;
                return true;
            }

            if (strCoordinateSystem.Equals("epsg:4619", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.SWEREF99;
                return true;
            }

            if (strCoordinateSystem.Equals("epsg:3035", StringComparison.OrdinalIgnoreCase))
            {
                coordinateSystem = CoordinateSys.ETRS89;
                return true;
            }

            coordinateSystem = default;
            return false;
        }

        #endregion Public
    }
}