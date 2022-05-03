using System;
using System.Runtime.CompilerServices;

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
        => InnerTypeInternal(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ITypeNode InnerTypeInternal(ITypeNode type)
    {
        if (type.Kind is SyntaxKind.NonNullType)
        {
            return ((NonNullTypeNode)type).Type;
        }

        if (type.Kind is SyntaxKind.ListType)
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
        ITypeNode innerType = InnerTypeInternal(type);

        if (innerType.Kind is SyntaxKind.NamedType)
        {
            return (NamedTypeNode)type;
        }

        for(var i = 0; i < 10; i++)
        {
            innerType = innerType.InnerType();

            if (innerType.Kind is SyntaxKind.NamedType)
            {
                return (NamedTypeNode)type;
            }
        }

        throw new NotSupportedException();
    }
}
