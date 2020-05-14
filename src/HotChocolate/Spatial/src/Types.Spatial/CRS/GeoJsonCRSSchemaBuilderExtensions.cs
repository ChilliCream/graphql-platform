using HotChocolate;

namespace HotChocolate.Types.Spatial.CRS
{
    public static class GeoJsonCRSSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddCRSTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<GeoJSONPointTypeExtension>()
                .AddType<GeoJSONMultiPointTypeExtension>()
                .AddType<GeoJSONLineStringTypeExtension>()
                .AddType<GeoJSONMultiLineStringTypeExtension>()
                .AddType<GeoJSONPolygonTypeExtension>()
                .AddType<GeoJSONMultiPolygonTypeExtension>()
                .AddType<GeoJSONInterfaceCrsExtension>();
        }
    }
}
