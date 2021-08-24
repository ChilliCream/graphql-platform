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
            IType type,
            object? coordinates,
            int? crs)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (coordinates is List<Coordinate> list)
            {
                coordinates = list.Count == 0
                    ? Array.Empty<Coordinate>()
                    : list.ToArray();
            }

            if (coordinates is not Coordinate[] coords)
            {
                throw Serializer_Parse_CoordinatesIsInvalid(type);
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

        public override object CreateInstance(IType type, object?[] fieldValues)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (fieldValues[0] is not GeoJsonGeometryType.Polygon)
            {
                throw Geometry_Parse_InvalidType(type);
            }

            return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (runtimeValue is not Geometry geometry)
            {
                throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
            }

            fieldValues[0] = GeoJsonGeometryType.Polygon;
            fieldValues[1] = geometry.Coordinates;
            fieldValues[2] = geometry.SRID;
        }

        public static readonly GeoJsonPolygonSerializer Default = new();
    }
}
