using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal static class AstUtilities
    {
        public static bool TryGetTypeFromAst<T>(
            this ISchema schema,
            ITypeNode typeNode,
            out T type)
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

        private static bool TryGetTypeFromAst(
            ISchema schema,
            ITypeNode typeNode,
            out IType type)
        {
            if (typeNode.Kind == NodeKind.NonNullType
                && TryGetTypeFromAst(schema,
                    ((NonNullTypeNode)typeNode).Type, out type))
            {
                type = new NonNullType(type);
                return true;
            }

            if (typeNode.Kind == NodeKind.ListType
                && TryGetTypeFromAst(schema,
                    ((ListTypeNode)typeNode).Type, out type))
            {
                type = new ListType(type);
                return true;
            }

            if (typeNode.Kind == NodeKind.NamedType
                && schema.TryGetType<INamedType>(
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
