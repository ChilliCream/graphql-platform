using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonMultiPointSerializer
        : GeoJsonInputObjectSerializer<MultiPoint>
    {
        private GeoJsonMultiPointSerializer()
            : base(GeoJsonGeometryType.MultiPoint)
        {
        }

        protected override bool TrySerializeCoordinates(
            Coordinate[] runtimeValue,
            [NotNullWhen(true)] out object? resultValue)
        {
            if (runtimeValue.Length == 1)
            {
                resultValue = GeoJsonPositionSerializer.Default.Serialize(runtimeValue[0]);
                return resultValue is {};
            }

            resultValue = null;
            return false;
        }

        public override MultiPoint CreateGeometry(
            object? coordinates,
            int? crs)
        {
            Point[]? geometries;

            if (coordinates is List<Coordinate> list)
            {
                geometries = new Point[list.Count];

                for (var i = 0; i < list.Count; i++)
                {
                    geometries[i] = GeoJsonPointSerializer.Default.CreateGeometry(list[i], crs);
                }

                goto Success;
            }

            if (coordinates is Coordinate[] parts)
            {
                geometries = new Point[parts.Length];

                for (var i = 0; i < parts.Length; i++)
                {
                    geometries[i] = GeoJsonPointSerializer.Default.CreateGeometry(parts[i], crs);
                }

                goto Success;
            }

            goto Error;

            Success:
            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);
                return factory.CreateMultiPoint(geometries);
            }

            return new MultiPoint(geometries);

            Error:
            throw Serializer_Parse_CoordinatesIsInvalid();
        }

        public override object CreateInstance(object?[] fieldValues)
        {
            if (fieldValues[0] is not GeoJsonGeometryType.MultiPoint)
            {
                throw Geometry_Parse_InvalidType();
            }

            return CreateGeometry(fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            var lineString = (LineString)runtimeValue;
            fieldValues[0] = GeoJsonGeometryType.MultiPoint;
            fieldValues[1] = lineString.Coordinates;
            fieldValues[2] = lineString.SRID;
        }

        public static readonly GeoJsonMultiPointSerializer Default = new();
    }
}
