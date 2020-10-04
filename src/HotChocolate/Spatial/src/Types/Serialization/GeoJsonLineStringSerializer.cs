using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonLineStringSerializer : GeoJsonInputObjectSerializer<LineString>
    {
        private GeoJsonLineStringSerializer()
            : base(GeoJsonGeometryType.LineString)
        {
        }

        public override LineString CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (!(coordinates is Coordinate[] coords) ||
                coords.Length < 2)
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateLineString(coords);
            }

            return new LineString(coords);
        }

        public static readonly GeoJsonLineStringSerializer Default =
            new GeoJsonLineStringSerializer();
    }
}
