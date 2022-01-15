using System;

namespace HotChocolate.Language;

public static class TypeNodeExtensions
{
    public static bool IsNonNullType(this ITypeNode type)
        => type.Kind == SyntaxKind.NonNullType;

    public static bool IsListType(this ITypeNode type)
    {
        if (type.Kind == SyntaxKind.ListType)
        {
            return true;
        }

        if (type.Kind == SyntaxKind.NonNullType &&
            ((NonNullTypeNode)type).Kind == SyntaxKind.ListType)
        {
            return true;
        }

        return false;
    }

    public static ITypeNode ElementType(this ITypeNode type)
    {
        if (type.Kind == SyntaxKind.NonNullType)
        {
            type = ((NonNullTypeNode)type).Type;
        }

        if (type.Kind == SyntaxKind.ListType)
        {
            return ((ListTypeNode)type).Type;
        }

        throw new InvalidOperationException();
    }

    public static ITypeNode InnerType(this ITypeNode type)
    {
        if (type.Kind == SyntaxKind.NonNullType)
        {
            return ((NonNullTypeNode)type).Type;
        }

        if (type.Kind == SyntaxKind.ListType)
        {
            return ((ListTypeNode)type).Type;
        }

        return type;
    }

    public static ITypeNode NullableType(this ITypeNode type)
    {
        if (type.Kind == SyntaxKind.NonNullType)
        {
            return ((NonNullTypeNode)type).Type;
        }

        return type;
    }

    public static NamedTypeNode NamedType(this ITypeNode type)
    {
        if (type.InnerType().InnerType().InnerType().InnerType().InnerType() is NamedTypeNode n)
        {
            return n;
        }

        throw new NotSupportedException();
    }

    public static bool IsEqualTo(this ITypeNode x, ITypeNode y)
    {
        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return x is null;
        }

        if (x is NonNullTypeNode nnx)
        {
            if (y is NonNullTypeNode nny)
            {
                return IsEqualTo(nnx.Type, nny.Type);
            }
            return false;
        }

        if (x is ListTypeNode lx)
        {
            if (y is ListTypeNode ly)
            {
                return IsEqualTo(lx.Type, ly.Type);
            }
            return false;
        }

        if (x is NamedTypeNode nx)
        {
            if (y is NamedTypeNode ny)
            {
                return nx.Name.Value.Equals(
                    ny.Name.Value,
                    StringComparison.Ordinal);
            }
            return false;
        }

        throw new NotSupportedException();
    }
}
