using HotChocolate;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;
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
                .AddType<GeoJSONGeometryType>()
                .AddType<PointObjectType>()
                .BindClrType<Coordinate, GeoJSONPositionScalar>();
        }

        public static ISchemaBuilder AddCRSTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<GeoJSONPointObjectExtensionType>()
                .AddType<GeoJSONMultiPointObjectExtensionType>()
                .AddType<GeoJSONInterfaceCrsExtension>();
        }
    }
}
