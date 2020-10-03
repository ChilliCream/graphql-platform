using HotChocolate.Types.Spatial;
using NetTopologySuite.Geometries;

namespace HotChocolate
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddSpatialTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<GeoJsonInterfaceType>()
                .AddType<GeoJsonGeometryType>()
                .AddType<GeoJsonPointInputType>()
                .AddType<GeoJsonMultiPointInputType>()
                .AddType<GeoJsonLineStringInputType>()
                .AddType<GeoJsonMultiLineStringInputType>()
                .AddType<GeoJsonPolygonInputType>()
                .AddType<GeoJsonMultiPolygonInputType>()
                .AddType<GeoJsonPointType>()
                .AddType<GeoJsonMultiPointType>()
                .AddType<GeoJsonLineStringType>()
                .AddType<GeoJsonMultiLineStringType>()
                .AddType<GeoJsonPolygonType>()
                .AddType<GeoJsonMultiPolygonType>()
                .AddType<GeoJsonGeometryEnumType>()
                .AddType<GeometryType>()
                .BindClrType<Coordinate, GeoJsonPositionType>();
        }
    }
}
