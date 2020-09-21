using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types
{
    internal class GeoJsonPointSerializer : GeoJsonInputObjectSerializer<Point>
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

        protected override bool IsCoordinateValid(object? coordinates)
        {
            return coordinates is Coordinate;
        }

        public override bool TryCreateGeometry(
            object? coordinates,
            int? crs,
            [NotNullWhen(true)] out Point? geometry)
        {
            if (!(coordinates is Coordinate coordinate))
            {
                geometry = null;
                return false;
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                geometry = factory.CreatePoint(coordinate);
                return true;
            }

            geometry = new Point(coordinate);
            return true;
        }

        protected override IValueNode ParseCoordinates(Coordinate[] runtimeValue)
        {
            return GeoJsonPositionSerializer.Default.ParseResult(runtimeValue[0]);
        }

        public static readonly GeoJsonPointSerializer Default = new GeoJsonPointSerializer();
    }
}
