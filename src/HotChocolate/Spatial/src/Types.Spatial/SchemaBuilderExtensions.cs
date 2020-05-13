using HotChocolate;
using NetTopologySuite.Geometries;
using Types.Spatial;
using Types.Spatial.Common;

namespace Types.Spatial
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

                .AddType<GeoJSONCoordinateReferenceSystemType>()

                .BindClrType<Coordinate, GeoJSONPositionScalar>();
        }

        public static ISchemaBuilder AddCRSTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<GeoJSONPointObjectExtensionType>()
                .AddType<GeoJSONMultiPointObjectExtensionType>()
                .AddType<GeoJSONLineStringObjectExtensionType>()
                .AddType<GeoJSONMultiLineStringObjectExtensionType>()
                .AddType<GeoJSONPolygonObjectExtensionType>()
                .AddType<GeoJSONMultiPolygonObjectExtensionType>()
                .AddType<GeoJSONInterfaceCrsExtension>();
        }
    }
}
