using HotChocolate;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;
using Types.Spatial.Input;
using Types.Spatial.Output;
using Types.Spatial.Scalar;

namespace Types.Spatial
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddSpatialTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<GeoJSONInterface>()
                .AddType<GeoJSONPointInput>()
                .AddType<GeoJSONGeometryType>()
                .AddType<GeoJSONPointObjectType>()
                .AddType<GeoJSONMultiPointObjectType>()
                .AddType<GeoJSONLineStringObjectType>()
                .AddType<GeoJSONMultiLineStringObjectType>()
                .AddType<GeoJSONPolygonObjectType>()
                .AddType<GeoJSONMultiPolygonObjectType>()
                .AddType<GeoJSONCoordinateReferenceSystemObjectType>()
                .BindClrType<Coordinate, GeoJSONPositionScalar>();
        }

        public static ISchemaBuilder AddCRSTypes(this ISchemaBuilder builder)
        {
            return builder
                .BindClrType<GeoJSONCoordinateReferenceSystem, GeoJSONCoordinateReferenceSystemScalar>()
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
