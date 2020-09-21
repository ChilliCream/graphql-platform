using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types
{
    internal class GeoJsonLineStringSerializer : GeoJsonInputObjectSerializer<LineString>
    {
        private GeoJsonLineStringSerializer()
            : base(GeoJsonGeometryType.LineString)
        {
        }

        protected override bool IsCoordinateValid(object? coordinates)
        {
            return coordinates is Coordinate[] c && c.Length > 1;
        }

        public override bool TryCreateGeometry(
            object? coordinates,
            int? crs,
            [NotNullWhen(true)] out LineString? geometry)
        {
            if (!(coordinates is Coordinate[] coords))
            {
                geometry = null;
                return false;
            }

            if (coords.Length < 2)
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                geometry = factory.CreateLineString(coords);
                return true;
            }

            geometry = new LineString(coords);
            return true;
        }

        public static readonly GeoJsonLineStringSerializer Default =
            new GeoJsonLineStringSerializer();
    }
}
