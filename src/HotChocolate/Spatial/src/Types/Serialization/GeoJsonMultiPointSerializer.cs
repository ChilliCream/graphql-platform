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
            if (!(coordinates is Coordinate[] parts))
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            var lineCount = parts.Length;
            var geometries = new Point[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                geometries[i] = GeoJsonPointSerializer.Default
                    .CreateGeometry(parts[i], crs);
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateMultiPoint(geometries);
            }

            return new MultiPoint(geometries);
        }

        public static readonly GeoJsonMultiPointSerializer Default =
            new GeoJsonMultiPointSerializer();
    }
}
