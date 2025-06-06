using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities;

[Obsolete("REMOVE THIS CLASS", error: true)]
public static class AstUtilities
{
    public static bool TryGetTypeFromAst<T>(this ISchemaDefinition schema, ITypeNode typeNode, out T type)
        where T : IType
    {
        if (TryGetTypeFromAst(schema, typeNode, out var internalType)
            && internalType is T t)
        {
            type = t;
            return true;
        }

        type = default;
        return false;
    }

    private static bool TryGetTypeFromAst(ISchemaDefinition schema, ITypeNode typeNode, out IType type)
    {
        if (typeNode.Kind == SyntaxKind.NonNullType
            && TryGetTypeFromAst(schema, ((NonNullTypeNode)typeNode).Type, out type))
        {
            type = new NonNullType(type);
            return true;
        }

        if (typeNode.Kind == SyntaxKind.ListType
            && TryGetTypeFromAst(schema, ((ListTypeNode)typeNode).Type, out type))
        {
            type = new ListType(type);
            return true;
        }

        if (typeNode.Kind == SyntaxKind.NamedType
            && schema.Types.TryGetType(((NamedTypeNode)typeNode).Name.Value, out var namedType))
        {
            type = namedType;
            return true;
        }

        type = null;
        return false;
    }
}
