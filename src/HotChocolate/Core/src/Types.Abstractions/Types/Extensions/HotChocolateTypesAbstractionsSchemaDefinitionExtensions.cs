using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class HotChocolateTypesAbstractionsSchemaDefinitionExtensions
{
    public static bool TryGetType<T>(
        this IReadOnlyTypeDefinitionCollection types,
        ITypeNode typeNode,
        [NotNullWhen(true)] out T? type)
        where T : IType
    {
        if (TryGetTypeFromAst(types, typeNode, out var internalType)
            && internalType is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    private static bool TryGetTypeFromAst(
        IReadOnlyTypeDefinitionCollection types,
        ITypeNode typeNode,
        [NotNullWhen(true)] out IType? type)
    {
        if (typeNode.Kind == SyntaxKind.NonNullType
            && TryGetTypeFromAst(types, ((NonNullTypeNode)typeNode).Type, out type))
        {
            type = new NonNullType(type);
            return true;
        }

        if (typeNode.Kind == SyntaxKind.ListType
            && TryGetTypeFromAst(types, ((ListTypeNode)typeNode).Type, out type))
        {
            type = new ListType(type);
            return true;
        }

        if (typeNode.Kind == SyntaxKind.NamedType
            && types.TryGetType(((NamedTypeNode)typeNode).Name.Value, out var namedType))
        {
            type = namedType;
            return true;
        }

        type = null;
        return false;
    }
}
