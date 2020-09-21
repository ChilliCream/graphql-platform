using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types
{
    internal class GeoJsonMultiPointSerializer : GeoJsonInputObjectSerializer<MultiPoint>
    {
        private GeoJsonMultiPointSerializer()
            : base(GeoJsonGeometryType.MultiPoint)
        {
        }

        protected override bool IsCoordinateValid(object? coordinates)
        {
            return coordinates is Coordinate[];
        }

        public override bool TryCreateGeometry(
            object? coordinates,
            int? crs,
            [NotNullWhen(true)] out MultiPoint? geometry)
        {
            if (!(coordinates is Coordinate[] parts))
            {
                geometry = null;
                return false;
            }

            var lineCount = parts.Length;
            var geometries = new Point[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                if (GeoJsonPointSerializer.Default.TryCreateGeometry(
                    parts[i],
                    crs,
                    out Point? point))
                {
                    geometries[i] = point;
                }
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                geometry = factory.CreateMultiPoint(geometries);
                return true;
            }

            geometry = new MultiPoint(geometries);
            return true;
        }

        public static readonly GeoJsonMultiPointSerializer Default =
            new GeoJsonMultiPointSerializer();
    }
}
