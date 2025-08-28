using HotChocolate.ModelContextProtocol.Types;

namespace HotChocolate.ModelContextProtocol.Extensions;

public static class SchemaBuilderExtensions
{
    public static ISchemaBuilder AddMcp(this ISchemaBuilder builder)
    {
        builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();

        return builder;
    }
}
