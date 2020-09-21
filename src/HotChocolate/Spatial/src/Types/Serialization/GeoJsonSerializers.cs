using System.Collections.Generic;
using HotChocolate.Types.Spatial;
using static HotChocolate.Types.Spatial.GeoJsonGeometryType;

namespace HotChocolate.Types
{
    internal static class GeoJsonSerializers
    {
        public static readonly IReadOnlyDictionary<GeoJsonGeometryType, IGeoJsonSerializer>
            Serializers =
                new Dictionary<GeoJsonGeometryType, IGeoJsonSerializer>
                {
                    { Point, GeoJsonPointSerializer.Default },
                    { MultiPoint, GeoJsonMultiPointSerializer.Default },
                    { LineString, GeoJsonLineStringSerializer.Default },
                    { MultiLineString, GeoJsonMultiLineStringSerializer.Default },
                    { Polygon, GeoJsonPolygonSerializer.Default },
                    { MultiPolygon, GeoJsonMultiPolygonSerializer.Default }
                };
    }
}
