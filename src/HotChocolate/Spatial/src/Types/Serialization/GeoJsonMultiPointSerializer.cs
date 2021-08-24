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
            IType type,
            Coordinate[] runtimeValue,
            [NotNullWhen(true)] out object? resultValue)
        {
            if (runtimeValue.Length == 1)
            {
                resultValue = GeoJsonPositionSerializer.Default.Serialize(type, runtimeValue[0]);
                return resultValue is {};
            }

            resultValue = null;
            return false;
        }

        public override MultiPoint CreateGeometry(
            IType type,
            object? coordinates,
            int? crs)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Point[]? geometries;

            if (coordinates is List<Coordinate> { Count: > 0 } list)
            {
                geometries = new Point[list.Count];

                for (var i = 0; i < list.Count; i++)
                {
                    geometries[i] =
                        GeoJsonPointSerializer.Default.CreateGeometry(type, list[i], crs);
                }

                goto Success;
            }

            if (coordinates is Coordinate[] { Length: > 0 } parts)
            {
                geometries = new Point[parts.Length];

                for (var i = 0; i < parts.Length; i++)
                {
                    geometries[i] =
                        GeoJsonPointSerializer.Default.CreateGeometry(type, parts[i], crs);
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
            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        public override object CreateInstance(IType type, object?[] fieldValues)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (fieldValues[0] is not GeoJsonGeometryType.MultiPoint)
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

            fieldValues[0] = GeoJsonGeometryType.MultiPoint;
            fieldValues[1] = geometry.Coordinates;
            fieldValues[2] = geometry.SRID;
        }

        public static readonly GeoJsonMultiPointSerializer Default = new();
    }
}
