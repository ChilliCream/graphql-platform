using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public static class TypeExtensions
    {
        public static bool IsNonNullType(this IType type)
        {
            return (type is NonNullType);
        }

        public static bool IsNonNullElementType(this IType type)
        {
            if (type is ListType l
                && l.ElementType is NonNullType)
            {
                return true;
            }

            if (type is NonNullType n
                && n.Type is ListType nl
                && nl.ElementType is NonNullType)
            {
                return true;
            }

            return false;
        }

        public static bool IsCompositeType(this IType type)
        {
            return IsType<ObjectType>(type)
                || IsType<UnionType>(type)
                || IsType<InterfaceType>(type);
        }

        public static bool IsLeafType(this IType type)
        {
            return IsScalarType(type)
                || IsEnumType(type);
        }

        public static bool IsListType(this IType type)
        {
            return IsType<ListType>(type);
        }

        public static bool IsScalarType(this IType type)
        {
            return IsType<ScalarType>(type);
        }

        public static bool IsObjectType(this IType type)
        {
            return IsType<ObjectType>(type);
        }

        public static bool IsEnumType(this IType type)
        {
            return IsType<EnumType>(type);
        }

        public static bool IsInterfaceType(this IType type)
        {
            return IsType<InterfaceType>(type);
        }

        public static bool IsInputObjectType(this IType type)
        {
            return IsType<InputObjectType>(type);
        }

        public static bool IsInputType(this IType type)
        {
            return type.InnerType().InnerType().InnerType() is IInputType;
        }

        public static bool IsOutputType(this IType type)
        {
            return type.InnerType().InnerType().InnerType() is IOutputType;
        }

        public static bool IsUnionType(this IType type)
        {
            return IsType<UnionType>(type);
        }

        public static bool IsAbstractType(this IType type)
        {
            return type.IsUnionType() || type.IsInterfaceType();
        }

        public static bool IsType<T>(this IType type)
            where T : IType
        {
            if (type is T)
            {
                return true;
            }

            if (type is NonNullType nnt
                && nnt.Type is T t)
            {
                return true;
            }

            return false;
        }

        public static IType InnerType(this IType type)
        {
            if (type is NonNullType n)
            {
                return n.Type;
            }

            if (type is ListType l)
            {
                return l.ElementType;
            }

            return type;
        }

        public static IType NullableType(this IType type)
        {
            if (type is NonNullType nnt)
            {
                return nnt.Type;
            }
            return type;
        }

        public static NameString TypeName(this IType type)
        {
            IType innerType = type.InnerType().InnerType().InnerType();

            if (innerType is INamedType nt)
            {
                return nt.Name;
            }

            // TODO : Resources
            throw new ArgumentException("The type structure is invalid.");
        }

        public static ListType ListType(this IType type)
        {
            if (type is ListType lt)
            {
                return lt;
            }

            if (type is NonNullType nnt && nnt.Type is ListType nnlt)
            {
                return nnlt;
            }

            // TODO : Resources
            throw new ArgumentException("The type structure is invalid.");
        }

        public static INamedType NamedType(this IType type)
        {
            IType innerType = type.InnerType().InnerType().InnerType();

            if (innerType is INamedType nt)
            {
                return nt;
            }

            throw new ArgumentException("The type structure is invalid.");
        }

        public static T NamedType<T>(this IType type)
            where T : INamedType
        {
            IType innerType = type.InnerType().InnerType().InnerType();

            if (innerType is T nt)
            {
                return nt;
            }

            // TODO : Resources
            throw new ArgumentException($"The type is not a {typeof(T).Name}.");
        }

        public static IType ElementType(this IType type)
        {
            if (type.IsListType())
            {
                if (type is ListType l)
                {
                    return l.ElementType;
                }
                else if (type is NonNullType n
                    && n.Type is ListType nl)
                {
                    return nl.ElementType;
                }

                // TODO : Resources
                throw new InvalidOperationException(
                    "The specified type is not a valid list type.");
            }

            // TODO : Resources
            throw new ArgumentException(
                "The specified type is not a list type.",
                nameof(type));
        }

        public static bool IsEqualTo(this IType x, IType y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is NonNullType xnn && y is NonNullType ynn)
            {
                return xnn.Type.IsEqualTo(ynn.Type);
            }
            else if (x is ListType xl && y is ListType yl)
            {
                return xl.ElementType.IsEqualTo(yl.ElementType);
            }
            else if (x is INamedType xnt && y is INamedType ynt)
            {
                return string.Equals(xnt.Name, ynt.Name,
                    StringComparison.Ordinal);
            }

            return false;
        }

        public static Type ToClrType(this IType type)
        {
            if (type.IsListType())
            {
                Type elementType = ToClrType(type.ElementType());
                return typeof(List<>).MakeGenericType(elementType);
            }

            if (type.IsLeafType())
            {
                return LeafTypeToClrType(type);
            }

            if (type.IsNonNullType())
            {
                return ToClrType(type.InnerType());
            }

            if (type is IHasClrType t && t.ClrType != null)
            {
                return t.ClrType;
            }

            return typeof(object);
        }

        private static Type LeafTypeToClrType(IType type)
        {
            if (type.IsLeafType() && type.NamedType() is IHasClrType t)
            {
                if (!type.IsNonNullType() && t.ClrType.IsValueType)
                {
                    return typeof(Nullable<>).MakeGenericType(t.ClrType);
                }
                return t.ClrType;
            }

            throw new NotSupportedException();
        }

        public static ITypeNode ToTypeNode(this IType type)
        {
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

            // TODO : Resources
            throw new NotSupportedException(
                "The specified type kind is not supported.");
        }
    }
}
