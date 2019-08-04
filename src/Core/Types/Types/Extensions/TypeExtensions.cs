using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public static class TypeExtensions
    {
        public static bool IsNonNullType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return (type is NonNullType);
        }

        public static bool IsCompositeType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<ObjectType>(type)
                || IsType<UnionType>(type)
                || IsType<InterfaceType>(type);
        }

        public static bool IsComplexType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<ObjectType>(type)
                || IsType<InterfaceType>(type);
        }

        public static bool IsLeafType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsScalarType(type)
                || IsEnumType(type);
        }

        public static bool IsListType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<ListType>(type);
        }

        public static bool IsScalarType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<ScalarType>(type);
        }

        public static bool IsObjectType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<ObjectType>(type);
        }

        public static bool IsEnumType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<EnumType>(type);
        }

        public static bool IsInterfaceType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<InterfaceType>(type);
        }

        public static bool IsInputObjectType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<InputObjectType>(type);
        }

        public static bool IsInputType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.NamedType() is IInputType;
        }

        public static bool IsOutputType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.NamedType() is IOutputType;
        }

        public static bool IsUnionType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return IsType<UnionType>(type);
        }

        public static bool IsAbstractType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsUnionType() || type.IsInterfaceType();
        }

        public static bool IsType<T>(this IType type)
            where T : IType
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type is NonNullType nnt)
            {
                return nnt.Type;
            }
            return type;
        }

        public static NameString TypeName(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.NamedType().Name;
        }

        public static ListType ListType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type is ListType lt)
            {
                return lt;
            }

            if (type is NonNullType nnt && nnt.Type is ListType nnlt)
            {
                return nnlt;
            }

            throw new ArgumentException(
                TypeResources.TypeExtensions_InvalidStructure);
        }

        public static INamedType NamedType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            IType current = type;

            if (current is INamedType n1)
            {
                return n1;
            }

            for (int i = 0; i < 6; i++)
            {
                current = current.InnerType();

                if (current is INamedType nn)
                {
                    return nn;
                }
            }

            throw new ArgumentException("The type structure is invalid.");
        }

        public static IType ElementType(this IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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
            }

            throw new ArgumentException(
                TypeResources.TypeExtensions_NoListType,
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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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
            if (type == null)
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
            if (typeNode is NonNullTypeNode nntn
                && ToType(nntn.Type, namedType) is INullableType nnt)
            {
                return new NonNullType(nnt);
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
    }
}
