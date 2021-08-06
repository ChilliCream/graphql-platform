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

        public override Point CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (coordinates is List<Coordinate> { Count: 1 } list)
            {
                coordinates = list[0];
            }

            if (coordinates is not Coordinate coordinate)
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreatePoint(coordinate);
            }

            return new Point(coordinate);
        }

        protected override IValueNode ParseCoordinates(IList runtimeValue)
        {
            if (runtimeValue.Count > 0 && runtimeValue[0] is IList ||
                runtimeValue is Coordinate[])
            {
                return GeoJsonPositionSerializer.Default.ParseResult(runtimeValue[0]);
            }

            return GeoJsonPositionSerializer.Default.ParseResult(runtimeValue);
        }

        public override object CreateInstance(object?[] fieldValues)
        {
            if (fieldValues[0] is not GeoJsonGeometryType.Point)
            {
                throw Geometry_Parse_InvalidType();
            }

            return CreateGeometry(fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            var lineString = (LineString)runtimeValue;
            fieldValues[0] = GeoJsonGeometryType.Point;
            fieldValues[1] = lineString.Coordinates;
            fieldValues[2] = lineString.SRID;
        }

        public static readonly GeoJsonPointSerializer Default = new();
    }
}
