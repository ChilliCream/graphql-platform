using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

public static class TypeExtensions
{
    public static int Depth(this IType type)
    {
        if (type is INamedType)
        {
            return 1;
        }
        return Depth(type.InnerType()) + 1;
    }

    public static bool IsNullableType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.Kind != TypeKind.NonNull;
    }

    public static bool IsNonNullType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.Kind == TypeKind.NonNull;
    }

    public static bool IsCompositeType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Object) ||
           IsType(type, TypeKind.Interface) ||
           IsType(type, TypeKind.Union);
    }

    public static bool IsComplexType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Object) ||
            IsType(type, TypeKind.Interface);
    }

    public static bool IsLeafType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Scalar) ||
            IsType(type, TypeKind.Enum);
    }

    public static bool IsListType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.List);
    }

    public static bool IsScalarType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return IsType(type, TypeKind.Scalar);
    }

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

    public static bool IsOutputType(this IType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.NamedType() is IOutputType;
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

        return type.Kind != TypeKind.NonNull ? type : ((NonNullType)type).Type;
    }

    public static NameString TypeName(this IType type)
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
            IType innerType = ((NonNullType)type).Type;
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

        IType current = type;

        if (IsNamed(current))
        {
            return (INamedType)current;
        }

        for (var i = 0; i < 6; i++)
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
            INamedType xnt when y is INamedType ynt => xnt.Name.Equals(ynt.Name),
            _ => false
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
            Type elementType = ToRuntimeType(type.ElementType());
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

        if (type is IHasRuntimeType { RuntimeType: { } } t)
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

        if (type is NonNullType nnt
            && ToTypeNode(nnt.Type) is INullableTypeNode nntn)
        {
            return new NonNullTypeNode(null, nntn);
        }

        if (type is ListType lt)
        {
            return new ListTypeNode(null, ToTypeNode(lt.ElementType));
        }

        if (type is INamedType nt)
        {
            return new NamedTypeNode(null, new NameNode(nt.Name));
        }

        throw new NotSupportedException(
            TypeResources.TypeExtensions_KindIsNotSupported);
    }

    public static ITypeNode ToTypeNode(
        this IType original,
        INamedType namedType)
    {
        if (original is NonNullType nnt
            && ToTypeNode(nnt.Type, namedType) is INullableTypeNode nntn)
        {
            return new NonNullTypeNode(null, nntn);
        }

        if (original is ListType lt)
        {
            return new ListTypeNode(null,
                ToTypeNode(lt.ElementType, namedType));
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
        if (typeNode is NonNullTypeNode nntn)
        {
            return new NonNullType(ToType(nntn.Type, namedType));
        }

        if (typeNode is ListTypeNode ltn)
        {
            return new ListType(ToType(ltn.Type, namedType));
        }

        if (typeNode is NamedTypeNode)
        {
            return namedType;
        }

        throw new NotSupportedException(
            TypeResources.TypeExtensions_KindIsNotSupported);
    }

    public static bool IsInstanceOfType(this IInputType type, IValueNode literal)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (literal is null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        if (literal.Kind == SyntaxKind.NullValue)
        {
            return type.Kind is not TypeKind.NonNull;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            return IsInstanceOfType((IInputType)((NonNullType)type).Type, literal);
        }

        if (type.Kind == TypeKind.List)
        {
            if (literal.Kind == SyntaxKind.ListValue)
            {
                var list = (ListValueNode)literal;

                if (list.Items.Count == 0)
                {
                    return true;
                }

                literal = list.Items[0];
            }

            return IsInstanceOfType((IInputType)((ListType)type).ElementType, literal);
        }

        if (type.Kind == TypeKind.InputObject)
        {
            return literal.Kind == SyntaxKind.ObjectValue;
        }

        return ((ILeafType)type).IsInstanceOfType(literal);
    }
}
