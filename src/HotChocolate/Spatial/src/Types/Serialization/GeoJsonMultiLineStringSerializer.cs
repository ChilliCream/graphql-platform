using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types
{
    internal class GeoJsonMultiLineStringSerializer
        : GeoJsonInputObjectSerializer<MultiLineString>
    {
        private GeoJsonMultiLineStringSerializer()
            : base(GeoJsonGeometryType.MultiLineString)
        {
        }

        public override MultiLineString CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (!(coordinates is Coordinate[][] parts))
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            var lineCount = parts.Length;
            var geometries = new LineString[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                geometries[i] = GeoJsonLineStringSerializer.Default
                    .CreateGeometry(parts[i], crs);
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateMultiLineString(geometries);
            }

            return new MultiLineString(geometries);
        }

        public static readonly GeoJsonMultiLineStringSerializer Default =
            new GeoJsonMultiLineStringSerializer();
    }
}
