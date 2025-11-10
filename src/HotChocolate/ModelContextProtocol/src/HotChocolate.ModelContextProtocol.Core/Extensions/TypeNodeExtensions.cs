using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Extensions;

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
