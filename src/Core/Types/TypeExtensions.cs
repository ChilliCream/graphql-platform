using System;

namespace HotChocolate.Types
{
    public static class TypeExtensions
    {
        public static bool TryGetKind(this IType type, out TypeKind kind)
        {
            if (IsNonNullType(type))
            {
                kind = TypeKind.NonNull;
                return true;
            }
            else if (IsListType(type))
            {
                kind = TypeKind.List;
                return true;
            }
            else if (IsObjectType(type))
            {
                kind = TypeKind.Object;
                return true;
            }
            else if (IsInterfaceType(type))
            {
                kind = TypeKind.Interface;
                return true;
            }
            else if (IsUnionType(type))
            {
                kind = TypeKind.Union;
                return true;
            }
            else if (IsEnumType(type))
            {
                kind = TypeKind.Enum;
                return true;
            }
            else if (IsScalarType(type))
            {
                kind = TypeKind.Scalar;
                return true;
            }
            else if (IsInputObjectType(type))
            {
                kind = TypeKind.InputObject;
                return true;
            }
            else
            {
                kind = default(TypeKind);
                return false;
            }
        }

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

        public static string TypeName(this IType type)
        {
            IType innerType = type.InnerType().InnerType().InnerType();

            if (innerType is INamedType nt)
            {
                return nt.Name;
            }

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

                throw new InvalidOperationException("The specified type is not a valid list type.");
            }

            throw new ArgumentException("The specified type is not a list type.", nameof(type));
        }
    }
}
