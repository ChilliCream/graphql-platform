using System;
using System.Collections.Generic;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonPolygonSerializer
        : GeoJsonInputObjectSerializer<Polygon>
    {
        private GeoJsonPolygonSerializer()
            : base(GeoJsonGeometryType.Polygon)
        {
        }

        public override Polygon CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (coordinates is List<Coordinate> list)
            {
                coordinates = list.Count == 0
                    ? Array.Empty<Coordinate>()
                    : list.ToArray();
            }

            if (coordinates is not Coordinate[] coords)
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                LinearRing ringSrid = factory.CreateLinearRing(coords);
                return factory.CreatePolygon(ringSrid);
            }

            var ring = new LinearRing(coords);
            return new Polygon(ring);
        }

        public override object CreateInstance(object?[] fieldValues)
        {
            if (fieldValues[0] is not GeoJsonGeometryType.Polygon)
            {
                throw Geometry_Parse_InvalidType();
            }

            return CreateGeometry(fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            var lineString = (LineString)runtimeValue;
            fieldValues[0] = GeoJsonGeometryType.Polygon;
            fieldValues[1] = lineString.Coordinates;
            fieldValues[2] = lineString.SRID;
        }

        public static readonly GeoJsonPolygonSerializer Default = new();
    }
}
