using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Extensions;

internal static class TypeNodeExtensions
{
    public static IType ToType(this ITypeNode typeNode, ISchemaDefinition schema)
    {
        var typeName = typeNode.NamedType().Name.Value;

        if (schema.Types.TryGetType(typeName, out var typeDefinition))
        {
            return typeNode.RewriteToType(typeDefinition);
        }

        throw new NotSupportedException(
            string.Format(TypeNodeExtensions_UnableToFindGraphQLTypeInSchema, typeName));
    }
}
