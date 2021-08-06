using System;
using System.Collections.Generic;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonLineStringSerializer : GeoJsonInputObjectSerializer<LineString>
    {
        private GeoJsonLineStringSerializer()
            : base(GeoJsonGeometryType.LineString)
        {
        }

        public override LineString CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (coordinates is List<Coordinate> list)
            {
                coordinates = list.Count == 0
                    ? Array.Empty<Coordinate>()
                    : list.ToArray();
            }

            if (coordinates is not Coordinate[] coords || coords.Length < 2)
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateLineString(coords);
            }

            return new LineString(coords);
        }

        public override object CreateInstance(object?[] fieldValues)
        {
            if (fieldValues[0] is not GeoJsonGeometryType.LineString)
            {
                throw Geometry_Parse_InvalidType();
            }

            return CreateGeometry(fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            var lineString = (LineString)runtimeValue;
            fieldValues[0] = GeoJsonGeometryType.LineString;
            fieldValues[1] = lineString.Coordinates;
            fieldValues[2] = lineString.SRID;
        }

        public static readonly GeoJsonLineStringSerializer Default = new();
    }
}
