using System;

namespace Prometheus.Abstractions
{
    public static class TypeExtensions
    {
        public static bool IsListType(this IType type)
        {
            if (type is ListType)
            {
                return true;
            }

            if (type is NonNullType n && n.Type is ListType)
            {
                return true;
            }

            return false;
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

            if (innerType is NamedType nt)
            {
                return nt.Name;
            }

            throw new ArgumentException("The type structure is invalid.");
        }

        public static NamedType NamedType(this IType type)
        {
            IType innerType = type.InnerType().InnerType().InnerType();

            if (innerType is NamedType nt)
            {
                return nt;
            }

            throw new ArgumentException("The type structure is invalid.");
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

        public static bool IsScalarType(this IType type)
        {
            if (type is NamedType nt && ScalarTypes.Contains(nt.Name))
            {
                return true;
            }
            else if (type is NonNullType nnt
                && nnt.Type is NamedType nnnt
                && (ScalarTypes.Contains(nnnt.Name)))
            {
                return true;
            }
            return false;
        }
    }
}