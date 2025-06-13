using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Properties;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130

public static class HotChocolateTypesAbstractionsTypeExtensions
{
    private const int MaxDepth = 16;

    /// <summary>
    /// Calculates the depth of a type. The depth is defined as the
    /// number of wrapper types + the named type itself.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns the depth of the type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static int Depth(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type is ITypeDefinition)
        {
            return 1;
        }

        return Depth(type.InnerType()) + 1;
    }

    /// <summary>
    /// Defines if a type is nullable.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is nullable; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsNullableType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Kind != TypeKind.NonNull;
    }

    /// <summary>
    /// Defines if a type is non-nullable.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is non-nullable; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsNonNullType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Kind == TypeKind.NonNull;
    }

    /// <summary>
    /// Defines if a type is an list type.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is an list type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsListType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.Kind switch
        {
            TypeKind.List => true,
            TypeKind.NonNull when ((NonNullType)type).NullableType.Kind == TypeKind.List => true,
            _ => false
        };
    }

    public static bool IsInputType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.NamedType().Kind
            is TypeKind.InputObject
            or TypeKind.Enum
            or TypeKind.Scalar;
    }

    public static bool IsOutputType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.NamedType().Kind
            is TypeKind.Interface
            or TypeKind.Object
            or TypeKind.Union
            or TypeKind.Enum
            or TypeKind.Scalar;
    }

    public static bool IsUnionType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Union);
    }

    public static bool IsAbstractType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Interface) || IsType(type, TypeKind.Union);
    }

    /// <summary>
    /// Defines if a type is a composite type (object, interface or union).
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is a composite type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsCompositeType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Object, TypeKind.Interface, TypeKind.Union);
    }

    /// <summary>
    /// Defines if a type is a complex type (object or interface).
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is a complex type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsComplexType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Object, TypeKind.Interface);
    }

    /// <summary>
    /// Defines if a type is a leaf type (scalar or enum).
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is a leaf type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsLeafType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Scalar, TypeKind.Enum);
    }

    /// <summary>
    /// Defines if a type is a scalar type.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is a scalar type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsScalarType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Scalar);
    }

    /// <summary>
    /// Defines if a type is an object type.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the type is an object type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public static bool IsObjectType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Object);
    }

    public static bool IsEnumType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Enum);
    }

    public static bool IsInterfaceType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.Interface);
    }

    public static bool IsInputObjectType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return IsType(type, TypeKind.InputObject);
    }

    public static bool IsNamedType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        switch (type.Kind)
        {
            case TypeKind.Enum:
            case TypeKind.InputObject:
            case TypeKind.Interface:
            case TypeKind.Object:
            case TypeKind.Scalar:
            case TypeKind.Union:
                return true;

            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsType(this IType type, TypeKind kind)
    {
        if (type.Kind == kind)
        {
            return true;
        }

        if (type.Kind == TypeKind.NonNull && ((NonNullType)type).NullableType.Kind == kind)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsType(this IType type, TypeKind kind1, TypeKind kind2)
    {
        if (type.Kind == kind1 || type.Kind == kind2)
        {
            return true;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            var innerKind = ((NonNullType)type).NullableType.Kind;

            if (innerKind == kind1 || innerKind == kind2)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsType(this IType type, TypeKind kind1, TypeKind kind2, TypeKind kind3)
    {
        if (type.Kind == kind1 || type.Kind == kind2 || type.Kind == kind3)
        {
            return true;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            var innerKind = ((NonNullType)type).NullableType.Kind;

            if (innerKind == kind1 || innerKind == kind2 || innerKind == kind3)
            {
                return true;
            }
        }

        return false;
    }

    public static IType InnerType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type switch
        {
            ListType listType => listType.ElementType,
            NonNullType nonNullType => nonNullType.NullableType,
            _ => type
        };
    }

    public static IType NullableType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.Kind == TypeKind.NonNull
            ? ((NonNullType)type).NullableType
            : type;
    }

    public static ListType ListType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Kind == TypeKind.List)
        {
            return (ListType)type;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            var innerType = ((NonNullType)type).NullableType;

            if (innerType.Kind == TypeKind.List)
            {
                return (ListType)innerType;
            }
        }

        throw new ArgumentException(TypesAbstractionResources.TypeExtensions_InvalidStructure);
    }

    public static IType ElementType(this IType type)
        => ListType(type).ElementType;

    public static ITypeNode ToType(this ITypeNode type, ITypeDefinition typeDefinition)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type switch
        {
            NamedTypeNode => new NamedTypeNode(typeDefinition.Name),
            ListTypeNode t => new ListTypeNode(ToType(t.Type, typeDefinition)),
            NonNullTypeNode t => new NonNullTypeNode((INullableTypeNode)ToType(t.Type, typeDefinition)),
            _ => throw new NotSupportedException()
        };
    }

    public static ITypeNode ToTypeNode(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type switch
        {
            ITypeDefinition namedType => new NamedTypeNode(namedType.Name),
            ListType listType => new ListTypeNode(ToTypeNode(listType.ElementType)),
            NonNullType nonNullType => new NonNullTypeNode((INullableTypeNode)ToTypeNode(nonNullType.NullableType)),
            _ => throw new NotSupportedException()
        };
    }

    public static ITypeNode ToTypeNode(
        this IType original,
        ITypeDefinition namedType)
    {
        if (original is NonNullType nonNullType
            && ToTypeNode(nonNullType.NullableType, namedType) is INullableTypeNode nullableTypeNode)
        {
            return new NonNullTypeNode(null, nullableTypeNode);
        }

        if (original is ListType listType)
        {
            return new ListTypeNode(
                null,
                ToTypeNode(listType.ElementType, namedType));
        }

        if (original is ITypeDefinition)
        {
            return new NamedTypeNode(null, new NameNode(namedType.Name));
        }

        throw new NotSupportedException(
            TypesAbstractionResources.TypeExtensions_KindIsNotSupported);
    }

    public static IType ReplaceNamedType(this IType type, Func<string, ITypeDefinition> newNamedType)
    {
        ArgumentNullException.ThrowIfNull(newNamedType);
        ArgumentNullException.ThrowIfNull(type);

        return type switch
        {
            ITypeDefinition namedType => newNamedType(namedType.Name),
            ListType listType => new ListType(ReplaceNamedType(listType.ElementType, newNamedType)),
            NonNullType nonNullType => new NonNullType(ReplaceNamedType(nonNullType.NullableType, newNamedType)),
            _ => throw new NotSupportedException()
        };
    }

    public static IType RewriteToType(this ITypeNode type, ITypeDefinition typeDefinition)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(typeDefinition);

        return type switch
        {
            NamedTypeNode => typeDefinition,
            ListTypeNode listTypeNode => new ListType(RewriteToType(listTypeNode.Type, typeDefinition)),
            NonNullTypeNode nonNullTypeNode => new NonNullType(RewriteToType(nonNullTypeNode.Type, typeDefinition)),
            _ => throw new NotSupportedException()
        };
    }

    public static string FullTypeName(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // if the type is a ITypeDefinition, we shortcut the type traversal
        // and simply return the name of the type.
        if (type is ITypeDefinition namedType)
        {
            return namedType.Name;
        }

        char[]? rented = null;
        Span<char> buffer = stackalloc char[128];
        int written;

        while (!FullTypeName(type, 0, buffer, out written))
        {
            var capacity = buffer.Length;

            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }

            rented = ArrayPool<char>.Shared.Rent(capacity * 2);
            buffer = rented;
        }

        var fullTypeName = new string(buffer[..written]);

        if (rented is not null)
        {
            rented.AsSpan()[..written].Clear();
            ArrayPool<char>.Shared.Return(rented);
        }

        return fullTypeName;
    }

    private static bool FullTypeName(IType type, int currentDepth, Span<char> buffer, out int written)
    {
        if (currentDepth > MaxDepth)
        {
            throw new InvalidOperationException(
                "The type resolution depth limit was exceeded.");
        }

        if (type is ITypeDefinition namedType)
        {
            if (buffer.Length < namedType.Name.Length)
            {
                written = 0;
                return false;
            }

            namedType.Name.AsSpan().CopyTo(buffer);
            written = namedType.Name.Length;
            return true;
        }

        if (type is ListType listType)
        {
            if (!FullTypeName(listType.ElementType, currentDepth + 1, buffer, out written))
            {
                return false;
            }

            if (buffer.Length < written + 2)
            {
                return false;
            }

            buffer[..written].CopyTo(buffer[1..]);

            buffer[0] = '[';
            buffer[written + 1] = ']';
            written += 2;
            return true;
        }

        if (type is NonNullType nonNullType)
        {
            if (!FullTypeName(nonNullType.NullableType, currentDepth + 1, buffer, out written))
            {
                return false;
            }

            if (buffer.Length < written + 1)
            {
                return false;
            }

            buffer[written] = '!';
            written += 1;
            return true;
        }

        throw new InvalidOperationException(
            "The specified type kind is not supported.");
    }

    /// <summary>
    /// Gets the named type (the most inner type) from a type structure.
    /// </summary>
    /// <param name="type">
    /// The type from which the named type shall be extracted.
    /// </param>
    /// <typeparam name="T">
    /// The expected type of the named type.
    /// </typeparam>
    /// <returns>
    /// Returns the named type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The type structure is invalid or
    /// the named type is not of the expected type.
    /// </exception>
    public static T NamedType<T>(this IType type) where T : ITypeDefinition
    {
        ArgumentNullException.ThrowIfNull(type);

        var namedType = type.NamedType();

        if (namedType is T t)
        {
            return t;
        }

        throw new ArgumentException(
            "The named type is not of the expected type.",
            nameof(type));
    }

    /// <summary>
    /// Gets the named type (the most inner type) from a type structure.
    /// </summary>
    /// <param name="type">
    /// The type from which the named type shall be extracted.
    /// </param>
    /// <returns>
    /// Returns the named type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The type structure is invalid.
    /// </exception>
    public static ITypeDefinition NamedType(this IType type)
        => type.AsTypeDefinition();

    public static ITypeDefinition AsTypeDefinition(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var depthRemaining = MaxDepth;

        if (type is ITypeDefinition typeDefinition)
        {
            return typeDefinition;
        }

        while (true)
        {
            if (depthRemaining-- <= 0)
            {
                throw new InvalidOperationException($"The type resolution depth limit of {MaxDepth} was exceeded.");
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    type = ((NonNullType)type).NullableType;
                    continue;

                case TypeKind.List:
                    type = ((ListType)type).ElementType;
                    continue;

                case TypeKind.Object:
                case TypeKind.Interface:
                case TypeKind.Union:
                case TypeKind.InputObject:
                case TypeKind.Enum:
                case TypeKind.Scalar:
                    return (ITypeDefinition)type;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    public static bool Equals(this IType thisType, IType? otherType, TypeComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(thisType);

        if (otherType is null)
        {
            return false;
        }

        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(thisType, otherType);
        }

        return thisType.IsStructurallyEqual(otherType);
    }

    [Obsolete("Use IsStructurallyEqual(this IType x, IType y) instead.")]
    public static bool IsEqualTo(this IType x, IType y)
        => x.IsStructurallyEqual(y);

    public static bool IsStructurallyEqual(this IType thisType, IType otherType)
    {
        var depthRemaining = MaxDepth;

        while (true)
        {
            if (depthRemaining-- <= 0)
            {
                throw new InvalidOperationException($"The type comparison depth limit of {MaxDepth} was reached.");
            }

            if (thisType.Kind != otherType.Kind)
            {
                return false;
            }

            if (thisType.Kind == TypeKind.NonNull)
            {
                thisType = ((NonNullType)thisType).NullableType;
                otherType = ((NonNullType)otherType).NullableType;
                continue;
            }

            if (thisType.Kind == TypeKind.List)
            {
                thisType = ((ListType)thisType).ElementType;
                otherType = ((ListType)otherType).ElementType;
                continue;
            }

            if (thisType.Kind == TypeKind.Object
                || thisType.Kind == TypeKind.Interface
                || thisType.Kind == TypeKind.Union
                || thisType.Kind == TypeKind.InputObject
                || thisType.Kind == TypeKind.Enum
                || thisType.Kind == TypeKind.Scalar)
            {
                return ((ITypeDefinition)thisType).Name.Equals(((ITypeDefinition)otherType).Name);
            }

            throw new InvalidOperationException("The specified type kind is not supported.");
        }
    }
}
