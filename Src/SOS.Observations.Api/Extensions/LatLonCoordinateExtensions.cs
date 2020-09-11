﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using SOS.Lib.Models.Gis;

namespace SOS.Observations.Api.Extensions
{
    public static class LatLonCoordinateExtensions
    {
        public static GeoLocation ToGeoLocation(this LatLonCoordinate coordinate)
        {
            return new GeoLocation(coordinate.Latitude, coordinate.Longitude);
        }
    }
}