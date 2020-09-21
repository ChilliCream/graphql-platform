using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Spatial;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types
{
    internal class GeoJsonMultiLineStringSerializer : GeoJsonInputObjectSerializer<MultiLineString>
    {
        private GeoJsonMultiLineStringSerializer()
            : base(GeoJsonGeometryType.MultiLineString)
        {
        }

        protected override bool IsCoordinateValid(object? coordinates)
        {
            return coordinates is Coordinate[][];
        }

        public override bool TryCreateGeometry(
            object? coordinates,
            int? crs,
            [NotNullWhen(true)] out MultiLineString? geometry)
        {
            if (!(coordinates is Coordinate[][] parts))
            {
                geometry = null;
                return false;
            }

            var lineCount = parts.Length;
            var geometries = new LineString[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                if (GeoJsonLineStringSerializer.Default.TryCreateGeometry(
                    parts[i],
                    crs,
                    out LineString? line))
                {
                    geometries[i] = line;
                }
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                geometry = factory.CreateMultiLineString(geometries);
                return true;
            }

            geometry = new MultiLineString(geometries);
            return true;
        }

        public static readonly GeoJsonMultiLineStringSerializer Default =
            new GeoJsonMultiLineStringSerializer();
    }
}
