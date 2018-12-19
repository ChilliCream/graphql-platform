using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class NamedTypeInfoFactory
        : ITypeInfoFactory
    {
        public bool TryCreate(Type type, out TypeInfo typeInfo)
        {
            if (CanHandle(type))
            {
                List<Type> components = DecomposeType(type);

                if (components.Any()
                    && (TryCreate4ComponentType(components, out typeInfo)
                    || TryCreate3ComponentType(components, out typeInfo)
                    || TryCreate2ComponentType(components, out typeInfo)
                    || TryCreate1ComponentType(components, out typeInfo)))
                {
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static List<Type> DecomposeType(Type type)
        {
            List<Type> components = new List<Type>();
            Type current = type;

            do
            {
                components.Add(current);
                current = GetInnerType(current);
            } while (current != null && components.Count < 4);

            if (IsTypeStackValid(components))
            {
                return components;
            }
            return new List<Type>();
        }

        private static bool IsTypeStackValid(List<Type> components)
        {
            foreach (Type type in components)
            {
                if (!CanHandle(type))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryCreate4ComponentType(
            List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 4
                && IsNonNullType(components[0])
                && IsListType(components[1])
                && IsNonNullType(components[2])
                && IsNamedType(components[3]))
            {
                typeInfo = new TypeInfo(components[3],
                    t => new NonNullType(new ListType(new NonNullType(t))));
                return true;
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate3ComponentType(
            List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 3)
            {
                if (IsListType(components[0])
                    && IsNonNullType(components[1])
                    && IsNamedType(components[2]))
                {
                    typeInfo = new TypeInfo(components[2],
                        t => new ListType(new NonNullType(t)));
                    return true;
                }

                if (IsNonNullType(components[0])
                    && IsListType(components[1])
                    && IsNamedType(components[2]))
                {
                    typeInfo = new TypeInfo(components[2],
                        t => new NonNullType(new ListType(t)));
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate2ComponentType(
            List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 2)
            {
                if (IsNonNullType(components[0])
                    && IsNamedType(components[1]))
                {
                    typeInfo = new TypeInfo(components[1],
                        t => new NonNullType(t));
                    return true;
                }

                if (IsListType(components[0])
                    && IsNamedType(components[1]))
                {
                    typeInfo = new TypeInfo(components[1],
                        t => new ListType(t));
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate1ComponentType(
            List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 1
               && IsNamedType(components[0]))
            {
                typeInfo = new TypeInfo(components[0], t => t);
                return true;
            }

            typeInfo = default;
            return false;
        }

        private static Type GetInnerType(Type type)
        {
            if (typeof(INamedType).IsAssignableFrom(type))
            {
                return null;
            }

            if (type.IsGenericType)
            {
                return type.GetGenericArguments().First();
            }

            return null;
        }

        private static bool IsListType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ListType<>);
        }

        private static bool IsNonNullType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(NonNullType<>);
        }

        private static bool IsNamedType(Type type)
        {
            return typeof(INamedType).IsAssignableFrom(type);
        }

        private static bool CanHandle(Type type)
        {
            return typeof(ScalarType).IsAssignableFrom(type)
                || typeof(ObjectType).IsAssignableFrom(type)
                || typeof(InterfaceType).IsAssignableFrom(type)
                || typeof(EnumType).IsAssignableFrom(type)
                || typeof(UnionType).IsAssignableFrom(type)
                || typeof(InputObjectType).IsAssignableFrom(type)
                || type.IsGenericType
                && (typeof(ListType<>) == type.GetGenericTypeDefinition()
                || typeof(NonNullType<>) == type.GetGenericTypeDefinition());
        }
    }
}
