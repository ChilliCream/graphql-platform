using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types
{
    internal class GeoJsonPolygonSerializer
        : GeoJsonInputObjectSerializer<Polygon>
    {
        private GeoJsonPolygonSerializer()
            : base(GeoJsonGeometryType.Polygon)
        {
        }

        public override Polygon CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (!(coordinates is Coordinate[] coords))
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                LinearRing ringSrid = factory.CreateLinearRing(coords);
                return factory.CreatePolygon(ringSrid);
            }

            var ring = new LinearRing(coords);
            return new Polygon(ring);
        }

        public static readonly GeoJsonPolygonSerializer Default =
            new GeoJsonPolygonSerializer();
    }
}
