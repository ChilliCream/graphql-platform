using HotChocolate.Adapters.Mcp.Directives;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

public static class SchemaBuilderExtensions
{
    public static ISchemaBuilder AddMcp(this ISchemaBuilder builder)
    {
        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        return builder;
    }
}
