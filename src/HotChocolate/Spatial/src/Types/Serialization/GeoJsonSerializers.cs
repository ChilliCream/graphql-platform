using System;
using System.Collections.Generic;
using HotChocolate.Types.Spatial;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types
{
    internal static class GeoJsonSerializers
    {
        public static readonly IReadOnlyDictionary<GeoJsonGeometryType, IGeoJsonSerializer>
            Serializers =
                new Dictionary<GeoJsonGeometryType, IGeoJsonSerializer>
                {
                    { GeoJsonGeometryType.Point, GeoJsonPointSerializer.Default },
                    { GeoJsonGeometryType.MultiPoint, GeoJsonMultiPointSerializer.Default },
                    { GeoJsonGeometryType.LineString, GeoJsonLineStringSerializer.Default },
                    {
                        GeoJsonGeometryType.MultiLineString,
                        GeoJsonMultiLineStringSerializer.Default
                    },
                    { GeoJsonGeometryType.Polygon, GeoJsonPolygonSerializer.Default },
                    { GeoJsonGeometryType.MultiPolygon, GeoJsonMultiPolygonSerializer.Default }
                };

        public static readonly IReadOnlyDictionary<Type, IGeoJsonSerializer>
            SerializersByType =
                new Dictionary<Type, IGeoJsonSerializer>
                {
                    { typeof(Point), GeoJsonPointSerializer.Default },
                    { typeof(MultiPoint), GeoJsonMultiPointSerializer.Default },
                    { typeof(LineString), GeoJsonLineStringSerializer.Default },
                    { typeof(MultiLineString), GeoJsonMultiLineStringSerializer.Default },
                    { typeof(Polygon), GeoJsonPolygonSerializer.Default },
                    { typeof(MultiPolygon), GeoJsonMultiPolygonSerializer.Default }
                };
    }
}
