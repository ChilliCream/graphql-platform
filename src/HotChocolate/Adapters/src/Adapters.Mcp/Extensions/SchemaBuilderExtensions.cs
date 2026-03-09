using HotChocolate.Adapters.Mcp.Directives;

namespace HotChocolate.Adapters.Mcp.Extensions;

public static class SchemaBuilderExtensions
{
    public static ISchemaBuilder AddMcp(this ISchemaBuilder builder)
    {
        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        return builder;
    }
}
