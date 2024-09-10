using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Provides utility methods to work with <see cref="IType"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Calculates the depth of a type. The depth is defined as the
    /// number of wrapper types + the named type itself..
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type is INamedType)
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.Kind == TypeKind.NonNull;
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Scalar, TypeKind.Enum);
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.List);
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Object);
    }

    public static bool IsEnumType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Enum);
    }

    public static bool IsInterfaceType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Interface);
    }

    public static bool IsInputObjectType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.InputObject);
    }

    public static bool IsInputType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.NamedType() is IInputType;
    }

    internal static IInputType EnsureInputType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.NamedType() is not IInputType)
        {
            throw InputTypeExpected(type);
        }

        return (IInputType)type;
    }

    public static bool IsOutputType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.NamedType() is IOutputType;
    }

    internal static IOutputType EnsureOutputType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.NamedType() is not IOutputType)
        {
            throw OutputTypeExpected(type);
        }

        return (IOutputType)type;
    }

    public static bool IsUnionType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Union);
    }

    public static bool IsAbstractType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Interface) || IsType(type, TypeKind.Union);
    }

    public static bool IsNamedType(this IType type)
    {
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

        if (type.Kind == TypeKind.NonNull && ((NonNullType)type).Type.Kind == kind)
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
            var innerKind = ((NonNullType)type).Type.Kind;

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
            var innerKind = ((NonNullType)type).Type.Kind;

            if (innerKind == kind1 || innerKind == kind2 || innerKind == kind3)
            {
                return true;
            }
        }

        return false;
    }

    public static IType InnerType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.Kind == TypeKind.NonNull)
        {
            return ((NonNullType)type).Type;
        }

        if (type.Kind == TypeKind.List)
        {
            return ((ListType)type).ElementType;
        }

        return type;
    }

    public static IType NullableType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.Kind != TypeKind.NonNull
            ? type
            : ((NonNullType)type).Type;
    }

    public static string TypeName(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.NamedType().Name;
    }

    public static ListType ListType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.Kind == TypeKind.List)
        {
            return (ListType)type;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            var innerType = ((NonNullType)type).Type;

            if (innerType.Kind == TypeKind.List)
            {
                return (ListType)innerType;
            }
        }

        throw new ArgumentException(TypeResources.TypeExtensions_InvalidStructure);
    }

    public static INamedType NamedType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var current = type;

        if (IsNamed(current))
        {
            return (INamedType)current;
        }

        const int maxDepth = 6;
        for (var i = 0; i < maxDepth; i++)
        {
            current = current.InnerType();

            if (IsNamed(current))
            {
                return (INamedType)current;
            }
        }

        throw new ArgumentException("The type structure is invalid.");

        static bool IsNamed(IType type)
        {
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
    }

    public static IType ElementType(this IType type)
        => ListType(type).ElementType;

    public static bool IsEqualTo(this IType x, IType y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        return x switch
        {
            NonNullType xnn when y is NonNullType ynn => xnn.Type.IsEqualTo(ynn.Type),
            ListType xl when y is ListType yl => xl.ElementType.IsEqualTo(yl.ElementType),
            INamedType xnt when y is INamedType ynt => xnt.Name.EqualsOrdinal(ynt.Name),
            _ => false,
        };
    }

    public static Type ToRuntimeType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsListType())
        {
            var elementType = ToRuntimeType(type.ElementType());
            return typeof(List<>).MakeGenericType(elementType);
        }

        if (type.IsLeafType())
        {
            return LeafTypeToClrType(type);
        }

        if (type.IsNonNullType())
        {
            return ToRuntimeType(type.InnerType());
        }

        if (type is IHasRuntimeType { RuntimeType: { }, } t)
        {
            return t.RuntimeType;
        }

        return typeof(object);
    }

    private static Type LeafTypeToClrType(IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsLeafType() && type.NamedType() is IHasRuntimeType t)
        {
            if (!type.IsNonNullType() && t.RuntimeType.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(t.RuntimeType);
            }

            return t.RuntimeType;
        }

        throw new NotSupportedException();
    }

    public static ITypeNode ToTypeNode(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type is NonNullType nonNullType && ToTypeNode(nonNullType.Type) is INullableTypeNode nullableTypeNode)
        {
            return new NonNullTypeNode(null, nullableTypeNode);
        }

        if (type is ListType listType)
        {
            return new ListTypeNode(null, ToTypeNode(listType.ElementType));
        }

        if (type is INamedType namedType)
        {
            return new NamedTypeNode(null, new NameNode(namedType.Name));
        }

        throw new NotSupportedException(
            TypeResources.TypeExtensions_KindIsNotSupported);
    }

    public static ITypeNode ToTypeNode(
        this IType original,
        INamedType namedType)
    {
        if (original is NonNullType nonNullType
            && ToTypeNode(nonNullType.Type, namedType) is INullableTypeNode nullableTypeNode)
        {
            return new NonNullTypeNode(null, nullableTypeNode);
        }

        if (original is ListType listType)
        {
            return new ListTypeNode(
                null,
                ToTypeNode(listType.ElementType, namedType));
        }

        if (original is INamedType)
        {
            return new NamedTypeNode(null, new NameNode(namedType.Name));
        }

        throw new NotSupportedException(
            TypeResources.TypeExtensions_KindIsNotSupported);
    }

    public static IType ToType(
        this ITypeNode typeNode,
        INamedType namedType)
    {
        if (typeNode is NonNullTypeNode nonNullTypeNode)
        {
            return new NonNullType(ToType(nonNullTypeNode.Type, namedType));
        }

        if (typeNode is ListTypeNode listTypeNode)
        {
            return new ListType(ToType(listTypeNode.Type, namedType));
        }

        if (typeNode is NamedTypeNode)
        {
            return namedType;
        }

        throw new NotSupportedException(
            TypeResources.TypeExtensions_KindIsNotSupported);
    }

    public static ITypeNode RenameName(this ITypeNode typeNode, string name)
        => typeNode switch
        {
            NonNullTypeNode nonNull => new NonNullTypeNode((INullableTypeNode)RenameName(nonNull.Type, name)),
            ListTypeNode list => new ListTypeNode(RenameName(list.Type, name)),
            NamedTypeNode named => named.WithName(named.Name.WithValue(name)),
            _ => throw new NotSupportedException(TypeResources.TypeExtensions_KindIsNotSupported),
        };

    public static bool IsInstanceOfType(this IInputType type, IValueNode literal)
    {
        while (true)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal.Kind is SyntaxKind.NullValue)
            {
                return type.Kind is not TypeKind.NonNull;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    type = (IInputType)((NonNullType)type).Type;
                    continue;

                case TypeKind.List:
                {
                    if (literal.Kind is SyntaxKind.ListValue)
                    {
                        var list = (ListValueNode)literal;

                        if (list.Items.Count == 0)
                        {
                            return true;
                        }

                        literal = list.Items[0];
                    }

                    type = (IInputType)((ListType)type).ElementType;
                    continue;
                }

                case TypeKind.InputObject:
                    return literal.Kind == SyntaxKind.ObjectValue;

                default:
                    return ((ILeafType)type).IsInstanceOfType(literal);
            }
        }
    }

    public static IType RewriteToNullableType(this IType type)
        => type.Kind is TypeKind.NonNull
            ? type.InnerType()
            : type;
}
