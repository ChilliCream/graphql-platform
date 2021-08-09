using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonPointSerializer
        : GeoJsonInputObjectSerializer<Point>
    {
        private GeoJsonPointSerializer()
            : base(GeoJsonGeometryType.Point)
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

        public override Point CreateGeometry(
            IType type,
            object? coordinates,
            int? crs)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (coordinates is List<Coordinate> { Count: 1 } list)
            {
                coordinates = list[0];
            }

            if (coordinates is not Coordinate coordinate)
            {
                throw Serializer_Parse_CoordinatesIsInvalid(type);
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreatePoint(coordinate);
            }

            return new Point(coordinate);
        }

        protected override IValueNode ParseCoordinates(IType type, IList runtimeValue)
        {
            if (runtimeValue.Count > 0 && runtimeValue[0] is IList ||
                runtimeValue is Coordinate[])
            {
                return GeoJsonPositionSerializer.Default.ParseResult(type, runtimeValue[0]);
            }

            return GeoJsonPositionSerializer.Default.ParseResult(type, runtimeValue);
        }

        public override object CreateInstance(IType type, object?[] fieldValues)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (fieldValues[0] is not GeoJsonGeometryType.Point)
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

            fieldValues[0] = GeoJsonGeometryType.Point;
            fieldValues[1] = geometry.Coordinates;
            fieldValues[2] = geometry.SRID;
        }

        public static readonly GeoJsonPointSerializer Default = new();
    }
}
