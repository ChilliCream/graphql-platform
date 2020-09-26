using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types
{
    internal class GeoJsonMultiPolygonSerializer
        : GeoJsonInputObjectSerializer<MultiPolygon>
    {
        private GeoJsonMultiPolygonSerializer()
            : base(GeoJsonGeometryType.MultiPolygon)
        {
        }

        public override MultiPolygon CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (!(coordinates is Coordinate[][] parts))
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            var lineCount = parts.Length;
            var geometries = new Polygon[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                geometries[i] = GeoJsonPolygonSerializer.Default
                    .CreateGeometry(parts[i], crs);
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateMultiPolygon(geometries);
            }

            return new MultiPolygon(geometries);
        }

        public static readonly GeoJsonMultiPolygonSerializer Default =
            new GeoJsonMultiPolygonSerializer();
    }
}
