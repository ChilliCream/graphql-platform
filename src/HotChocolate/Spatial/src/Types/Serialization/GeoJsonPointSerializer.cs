using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types
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
            if (!(coordinates is Coordinate coordinate))
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreatePoint(coordinate);
            }

            return new Point(coordinate);
        }

        protected override IValueNode ParseCoordinates(IList runtimeValue)
        {
            return GeoJsonPositionSerializer.Default.ParseResult(runtimeValue[0]);
        }

        public static readonly GeoJsonPointSerializer Default = new GeoJsonPointSerializer();
    }
}
