using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types
{
    internal class GeoJsonMultiPolygonSerializer : GeoJsonInputObjectSerializer<MultiPolygon>
    {
        private GeoJsonMultiPolygonSerializer()
            : base(GeoJsonGeometryType.MultiPolygon)
        {
        }

        protected override bool IsCoordinateValid(object? coordinates)
        {
            return coordinates is Coordinate[][];
        }

        public override bool TryCreateGeometry(
            object? coordinates,
            int? crs,
            [NotNullWhen(true)] out MultiPolygon? geometry)
        {
            if (!(coordinates is Coordinate[][] parts))
            {
                geometry = null;
                return false;
            }

            var lineCount = parts.Length;
            var geometries = new Polygon[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                if (GeoJsonPolygonSerializer.Default.TryCreateGeometry(
                    parts[i],
                    crs,
                    out Polygon? line))
                {
                    geometries[i] = line;
                }
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                geometry = factory.CreateMultiPolygon(geometries);
                return true;
            }

            geometry = new MultiPolygon(geometries);
            return true;
        }

        public static readonly GeoJsonMultiPolygonSerializer Default =
            new GeoJsonMultiPolygonSerializer();
    }
}
