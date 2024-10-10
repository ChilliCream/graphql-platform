using System.Runtime.CompilerServices;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public static class SyntaxNodeExtensions
{
    /// <summary>
    /// Specifies if the current value node represents <c>null</c>.
    /// </summary>
    /// <param name="value">
    /// The value node.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the current value node represents <c>null</c>;
    /// otherwise, <c>false</c> is returned.
    /// </returns>
    public static bool IsNull(this IValueNode? value)
        => value is null or NullValueNode;

    public static bool IsNonNullType(this ITypeNode type)
        => type.Kind is SyntaxKind.NonNullType;

    public static bool IsListType(this ITypeNode type)
    {
        if (type.Kind is SyntaxKind.ListType)
        {
            return true;
        }

        if (type.Kind is SyntaxKind.NonNullType &&
            ((NonNullTypeNode)type).Type.Kind is SyntaxKind.ListType)
        {
            return true;
        }

        return false;
    }

    public static ITypeNode ElementType(this ITypeNode type)
    {
        if (type.Kind is SyntaxKind.NonNullType)
        {
            type = ((NonNullTypeNode)type).Type;
        }

        if (type.Kind is SyntaxKind.ListType)
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
        if (type.Kind is SyntaxKind.NonNullType)
        {
            return ((NonNullTypeNode)type).Type;
        }

        return type;
    }

    public static NamedTypeNode NamedType(this ITypeNode type)
    {
        var innerType = InnerTypeInternal(type);

        if (innerType.Kind is SyntaxKind.NamedType)
        {
            return (NamedTypeNode)innerType;
        }

        for(var i = 0; i < 10; i++)
        {
            innerType = innerType.InnerType();

            if (innerType.Kind is SyntaxKind.NamedType)
            {
                return (NamedTypeNode)innerType;
            }
        }

        throw new NotSupportedException();
    }

    public static bool Equals(
        this ISyntaxNode node,
        ISyntaxNode? other,
        SyntaxComparison comparison)
        => comparison is SyntaxComparison.Syntax
            ? SyntaxComparer.BySyntax.Equals(node, other)
            : SyntaxComparer.ByReference.Equals(node, other);

    public static string ToString(this ISyntaxNode node, SyntaxSerializerOptions options)
    {
        var serializer = new SyntaxSerializer(options);
        var writer = StringSyntaxWriter.Rent();

        try
        {
            serializer.Serialize(node, writer);
            return writer.ToString();
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }
}
