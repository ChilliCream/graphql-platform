using HotChocolate.Types.Spatial;
using NetTopologySuite.Geometries;

namespace HotChocolate
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddSpatialTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<GeoJSONInterface>()
                .AddType<GeoJSONGeometryType>()

                .AddType<GeoJSONPointInput>()
                .AddType<GeoJSONMultiPointInput>()
                .AddType<GeoJSONLineStringInput>()
                .AddType<GeoJSONMultiLineStringInput>()
                .AddType<GeoJSONPolygonInput>()
                .AddType<GeoJSONMultiPolygonInput>()

                .AddType<GeoJSONPointType>()
                .AddType<GeoJSONMultiPointType>()
                .AddType<GeoJSONLineStringType>()
                .AddType<GeoJSONMultiLineStringType>()
                .AddType<GeoJSONPolygonType>()
                .AddType<GeoJSONMultiPolygonType>()

                .BindClrType<Coordinate, GeoJSONPositionScalar>();
        }
    }
}
