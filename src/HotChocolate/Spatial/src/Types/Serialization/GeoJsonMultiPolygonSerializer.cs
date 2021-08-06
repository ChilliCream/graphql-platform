using System;
using System.Collections.Generic;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonMultiPolygonSerializer
        : GeoJsonInputObjectSerializer<MultiPolygon>
    {
        private GeoJsonMultiPolygonSerializer()
            : base(GeoJsonGeometryType.MultiPolygon)
        {
        }

        public override MultiPolygon CreateGeometry(
            object? coordinates,
            int? crs)
        {
            Polygon[]? geometries;

            if (coordinates is List<List<Coordinate>> list)
            {
                geometries = new Polygon[list.Count];

                for (var i = 0; i < list.Count; i++)
                {
                    geometries[i] =
                        GeoJsonPolygonSerializer.Default.CreateGeometry(list[i].ToArray(), crs);
                }

                goto Success;
            }

            if (coordinates is Coordinate[][] parts)
            {
                geometries = new Polygon[parts.Length];

                for (var i = 0; i < parts.Length; i++)
                {
                    geometries[i] =
                        GeoJsonPolygonSerializer.Default.CreateGeometry(parts[i], crs);
                }

                goto Success;
            }

            goto Error;

            Success:
            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateMultiPolygon(geometries);
            }

            return new MultiPolygon(geometries);

            Error:
            throw Serializer_Parse_CoordinatesIsInvalid();
        }

        public override object CreateInstance(object?[] fieldValues)
        {
            if (fieldValues[0] is not GeoJsonGeometryType.MultiPolygon)
            {
                throw Geometry_Parse_InvalidType();
            }

            return CreateGeometry(fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            var lineString = (LineString)runtimeValue;
            fieldValues[0] = GeoJsonGeometryType.MultiPolygon;
            fieldValues[1] = lineString.Coordinates;
            fieldValues[2] = lineString.SRID;
        }

        public static readonly GeoJsonMultiPolygonSerializer Default = new();
    }
}
