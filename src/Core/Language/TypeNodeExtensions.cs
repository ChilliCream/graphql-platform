using System;

namespace HotChocolate.Language
{
    public static class TypeNodeExtensions
    {
        public static bool IsNonNullType(this ITypeNode type)
        {
            return type is NonNullTypeNode;
        }

        public static bool IsListType(this ITypeNode type)
        {
            return type is ListTypeNode;
        }

        public static ITypeNode InnerType(this ITypeNode type)
        {
            if (type is NonNullTypeNode n)
            {
                return n.Type;
            }

            if (type is ListTypeNode l)
            {
                return l.Type;
            }

            return type;
        }

        public static ITypeNode NullableType(this ITypeNode type)
        {
            if (type is NonNullTypeNode n)
            {
                return n.Type;
            }

            return type;
        }

        public static NamedTypeNode NamedType(this ITypeNode type)
        {
            if (type.InnerType().InnerType().InnerType() is NamedTypeNode n)
            {
                return n;
            }

            throw new NotSupportedException();
        }
    }
}
