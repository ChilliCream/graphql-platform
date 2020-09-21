using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types
{
    internal class GeoJsonPolygonSerializer : GeoJsonInputObjectSerializer<Polygon>
    {
        private GeoJsonPolygonSerializer()
            : base(GeoJsonGeometryType.Polygon)
        {
        }

        protected override bool IsCoordinateValid(object? coordinates)
        {
            return coordinates is Coordinate[];
        }

        public override bool TryCreateGeometry(
            object? coordinates,
            int? crs,
            [NotNullWhen(true)] out Polygon? geometry)
        {
            if (!(coordinates is Coordinate[] coords))
            {
                geometry = null;
                return false;
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                LinearRing ringSrid = factory.CreateLinearRing(coords);
                geometry = factory.CreatePolygon(ringSrid);
                return true;
            }

            var ring = new LinearRing(coords);
            geometry = new Polygon(ring);
            return true;
        }

        public static readonly GeoJsonPolygonSerializer Default =
            new GeoJsonPolygonSerializer();
    }
}
