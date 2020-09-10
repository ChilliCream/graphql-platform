using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    public static class AstUtilities
    {
        public static bool TryGetTypeFromAst<T>(this ISchema schema, ITypeNode typeNode, out T type)
            where T : IType
        {
            if (TryGetTypeFromAst(schema, typeNode, out IType internalType)
                && internalType is T t)
            {
                type = t;
                return true;
            }

            type = default;
            return false;
        }

        private static bool TryGetTypeFromAst(ISchema schema, ITypeNode typeNode, out IType type)
        {
            if (typeNode.Kind == SyntaxKind.NonNullType
                && TryGetTypeFromAst(schema,
                    ((NonNullTypeNode)typeNode).Type, out type))
            {
                type = new NonNullType(type);
                return true;
            }

            if (typeNode.Kind == SyntaxKind.ListType
                && TryGetTypeFromAst(schema,
                    ((ListTypeNode)typeNode).Type, out type))
            {
                type = new ListType(type);
                return true;
            }

            if (typeNode.Kind == SyntaxKind.NamedType
                && schema.TryGetType(
                    ((NamedTypeNode)typeNode).Name.Value,
                    out INamedType namedType))
            {
                type = namedType;
                return true;
            }

            type = default;
            return false;
        }
    }
}
