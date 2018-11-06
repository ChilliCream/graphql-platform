using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class DotNetTypeInfoFactory
        : ITypeInfoFactory
    {
        public bool TryCreate(Type type, out TypeInfo typeInfo)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (CanHandle(type))
            {
                List<Type> components = DecomposeType(type);

                if (components.Any()
                    && (TryCreate3ComponentType(components, out typeInfo)
                    || TryCreate2ComponentType(components, out typeInfo)
                    || TryCreate1ComponentType(components, out typeInfo)))
                {
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate3ComponentType(
             List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 3
                && IsListType(components[0])
                && IsNullableType(components[1])
                && components[2].IsValueType)
            {
                typeInfo = new TypeInfo(components[2], t => new ListType(t));
                return true;
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate2ComponentType(
             List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 2)
            {
                if (IsListType(components[0])
                    && components[1].IsValueType)
                {
                    typeInfo = new TypeInfo(components[1], t => new ListType(new NonNullType(t)));
                    return true;
                }

                if (IsListType(components[0])
                    && IsPossibleNamedType(components[1]))
                {
                    typeInfo = new TypeInfo(components[1], t => new ListType(t));
                    return true;
                }

                if (IsNullableType(components[0])
                    && components[1].IsValueType)
                {
                    typeInfo = new TypeInfo(components[1], t => t);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate1ComponentType(
             List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 1)
            {
                if (components[0].IsValueType)
                {
                    typeInfo = new TypeInfo(components[0], t => new NonNullType(t));
                    return true;
                }

                if (IsPossibleNamedType(components[0]))
                {
                    typeInfo = new TypeInfo(components[0], t => t);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static List<Type> DecomposeType(Type type)
        {
            List<Type> components = new List<Type>();
            Type current = RemoveNonEssentialParts(type);

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

        private static Type RemoveNonEssentialParts(Type type)
        {
            Type current = type;

            if (IsWrapperType(current))
            {
                current = GetInnerType(current);
            }

            if (IsTaskType(current))
            {
                current = GetInnerType(current);
            }

            if (IsResoverResultType(current))
            {
                current = GetInnerType(current);
            }

            return current;
        }

        private static bool IsTypeStackValid(List<Type> components)
        {
            foreach (Type type in components)
            {
                if (typeof(Task).IsAssignableFrom(type))
                {
                    return false;
                }

                if (typeof(IType).IsAssignableFrom(type))
                {
                    return false;
                }
            }
            return true;
        }

        private static Type GetInnerType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (IsTaskType(type)
                || IsNullableType(type)
                || IsWrapperType(type)
                || IsResoverResultType(type))
            {
                return type.GetGenericArguments().First();
            }

            if (ImplementsIList(type))
            {
                return GetInnerListType(type);
            }

            return null;
        }

        private static Type GetInnerListType(Type type)
        {
            if (type.IsInterface && IsSupportedCollectionInterface(type, true))
            {
                return type.GetGenericArguments().First();
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (IsSupportedCollectionInterface(interfaceType))
                {
                    return interfaceType.GetGenericArguments().First();
                }
            }
            return null;
        }

        private static bool IsSupportedCollectionInterface(Type type) =>
            IsSupportedCollectionInterface(type, false);

        private static bool IsSupportedCollectionInterface(
            Type type,
            bool allowEnumerable)
        {
            if (type.IsGenericType)
            {
                Type typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IReadOnlyCollection<>)
                    || typeDefinition == typeof(IReadOnlyList<>)
                    || typeDefinition == typeof(ICollection<>)
                    || typeDefinition == typeof(IList<>))
                {
                    return true;
                }

                if (allowEnumerable && typeDefinition == typeof(IEnumerable<>))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsListType(Type type)
        {
            return type.IsArray
                || typeof(ListType) == type
                || ImplementsIList(type);
        }

        private static bool IsTaskType(Type type)
        {
            return type.IsGenericType
                && typeof(Task<>) == type.GetGenericTypeDefinition();
        }

        private static bool IsResoverResultType(Type type)
        {
            return type.IsGenericType
                && (typeof(IResolverResult<>) == type.GetGenericTypeDefinition()
                || typeof(ResolverResult<>) == type.GetGenericTypeDefinition());
        }

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType
                && typeof(Nullable<>) == type.GetGenericTypeDefinition();
        }

        public static bool IsPossibleNamedType(Type type)
        {
            return !IsNullableType(type)
                && !IsTaskType(type)
                && !IsListType(type)
                && !IsWrapperType(type);
        }

        private static bool ImplementsIList(Type type)
        {
            return GetInnerListType(type) != null;
        }

        private static bool IsWrapperType(Type type)
        {
            return type.IsGenericType
                && typeof(NativeType<>) == type.GetGenericTypeDefinition();
        }

        private static bool CanHandle(Type type)
        {
            return !typeof(IType).IsAssignableFrom(type)
                || IsWrapperType(type);
        }
    }
}
