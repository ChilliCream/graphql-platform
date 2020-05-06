using HotChocolate;
using Types.Spatial.Input;
using Types.Spatial.Output;

namespace Types.Spatial
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddSpatialTypes(this ISchemaBuilder builder)
        {
            return builder
                .AddType<PointInputObject>()
                .AddType<PointObjectType>()
                .AddType<LineStringInputObject>();
        }
    }
}
