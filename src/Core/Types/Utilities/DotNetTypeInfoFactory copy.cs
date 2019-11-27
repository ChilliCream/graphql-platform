using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    internal class DotNetTypeInfoFactory1
        : ITypeInfoFactory
    {
        public bool TryCreate(IExtendedType type, out TypeInfo? typeInfo)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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

        public static Type Unwrap(Type type)
        {
            return RemoveNonEssentialParts(type);
        }

        public static Type Rewrite(
            Type type,
            bool isNonNullType,
            bool isNonNullElementType)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (CanHandle(type))
            {
                var components = RemoveNonNullComponents(type).ToList();

                if (components.Count == 2
                    && IsListType(components[0])
                    && IsPossibleNamedType(components[1]))
                {
                    return RewriteListType(
                        components[1],
                        isNonNullType,
                        isNonNullElementType);
                }

                if (components.Count == 1)
                {
                    return RewriteNamedType(components[0], isNonNullType);
                }
            }

            return type;
        }


        private static IEnumerable<Type> RemoveNonNullComponents(Type type)
        {
            foreach (Type component in DecomposeType(type))
            {
                if (!IsNonNullType(component) && !IsNullableType(component))
                {
                    yield return component;
                }
            }
        }

        private static Type RewriteListType(
            Type elementType,
            bool isNonNullType,
            bool isNonNullElementType)
        {
            Type newType = RewriteNamedType(
                elementType,
                isNonNullElementType);

            newType = MakeListType(newType);
            if (isNonNullType)
            {
                newType = MakeNonNullType(newType);
            }

            return newType;
        }

        private static Type RewriteNamedType(
            Type namedType,
            bool isNonNullType)
        {
            Type newType = namedType;

            if (isNonNullType)
            {
                newType = MakeNonNullType(newType);
            }
            else if (newType.IsValueType)
            {
                newType = MakeNullableType(newType);
            }

            return newType;
        }

        private static Type MakeNullableType(Type valueType)
        {
            return typeof(Nullable<>).MakeGenericType(valueType);
        }

        private static Type MakeListType(Type elementType)
        {
            return typeof(List<>).MakeGenericType(elementType);
        }

        private static Type MakeNonNullListType(Type elementType)
        {
            return MakeNonNullType(MakeListType(elementType));
        }

        private static Type MakeNonNullType(Type nullableType)
        {
            Type wrapper = typeof(NativeType<>).MakeGenericType(nullableType);
            return typeof(NonNullType<>).MakeGenericType(wrapper);
        }

        private static bool TryCreate4ComponentType(
            List<Type> components, out TypeInfo typeInfo)
        {
            if (components.Count == 4)
            {
                if (IsNonNullType(components[0])
                    && IsListType(components[1])
                    && IsNonNullType(components[2])
                    && IsPossibleNamedType(components[3]))
                {
                    typeInfo = new TypeInfo(
                        components[3],
                        components,
                        t => new NonNullType(new ListType(new NonNullType(t))));
                    return true;
                }

                if (IsNonNullType(components[0])
                    && IsListType(components[1])
                    && IsNullableType(components[2])
                    && components[3].IsValueType)
                {
                    typeInfo = new TypeInfo(
                        components[2],
                        components,
                        t => new NonNullType(new ListType(t)));
                    return true;
                }
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
                    && IsNullableType(components[1])
                    && components[2].IsValueType)
                {
                    typeInfo = new TypeInfo(
                        components[2],
                        components,
                        t => new ListType(t));
                    return true;
                }

                if (IsListType(components[0])
                    && IsNonNullType(components[1])
                    && IsPossibleNamedType(components[2]))
                {
                    typeInfo = new TypeInfo(
                        components[2],
                        components,
                        t => new ListType(new NonNullType(t)));
                    return true;
                }

                if (IsNonNullType(components[0])
                    && IsListType(components[1])
                    && IsPossibleNamedType(components[2]))
                {
                    typeInfo = new TypeInfo(
                        components[2],
                        components,
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
                if (IsListType(components[0])
                    && components[1].IsValueType)
                {
                    typeInfo = new TypeInfo(
                        components[1],
                        components,
                        t => new ListType(new NonNullType(t)));
                    return true;
                }

                if (IsListType(components[0])
                    && IsPossibleNamedType(components[1]))
                {
                    typeInfo = new TypeInfo(
                        components[1],
                        components,
                        t => new ListType(t));
                    return true;
                }

                if (IsNullableType(components[0])
                    && components[1].IsValueType)
                {
                    typeInfo = new TypeInfo(
                        components[1],
                        components,
                        t => t);
                    return true;
                }

                if (IsNonNullType(components[0])
                    && IsPossibleNamedType(components[1]))
                {
                    typeInfo = new TypeInfo(
                        components[1],
                        components,
                        t => new NonNullType(t));
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
                    typeInfo = new TypeInfo(
                        components[0],
                        components,
                        t => new NonNullType(t));
                    return true;
                }

                if (IsPossibleNamedType(components[0]))
                {
                    typeInfo = new TypeInfo(
                        components[0],
                        components,
                        t => t);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static List<TypeComponent> DecomposeType(IExtendedType type)
        {
            var components = new List<TypeComponent>();
            IExtendedType? current = type;

            do
            {
                current = RemoveNonEssentialParts(current);

                if (!current.IsNullable)
                {
                    components.Add(TypeComponent.NonNull);
                }

                if (IsListType(current))
                {
                    components.Add(TypeComponent.List);
                }

                current = GetInnerType(current);
            } while (current != null && components.Count < 5);

            if (IsTypeStackValid(components))
            {
                return components;
            }

            return new List<TypeComponent>();
        }

        private static IExtendedType RemoveNonEssentialParts(IExtendedType type)
        {
            IExtendedType current = type;

            if (IsTaskType(current))
            {
                current = current.TypeArguments[0];
            }

            if (IsResolverResultType(current))
            {
                current = current.TypeArguments[0];
            }

            return current;
        }

        private static bool IsTypeStackValid(List<TypeComponent> components)
        {
            foreach (TypeComponent component in components)
            {
                if (typeof(Task).IsAssignableFrom(component.Type))
                {
                    return false;
                }

                if (typeof(INamedType).IsAssignableFrom(component.Type))
                {
                    return false;
                }
            }
            return true;
        }

        private static IExtendedType? GetInnerType(IExtendedType type)
        {
            if (type.IsArray)
            {
                return type.TypeArguments[0];
            }

            if (IsTaskType(type)
                || IsNonNullType(type)
                || IsResolverResultType(type))
            {
                return type.TypeArguments[0];
            }

            if (ImplementsListInterface(type))
            {
                return GetElementType(type);
            }

            return null;
        }

        private static IExtendedType? GetElementType(IExtendedType type)
        {
            if (type.IsInterface && IsSupportedCollectionInterface(type, true))
            {
                return type.TypeArguments[0];
            }

            foreach (IExtendedType interfaceType in type.GetInterfaces())
            {
                if (IsSupportedCollectionInterface(interfaceType))
                {
                    return interfaceType.TypeArguments[0];
                }
            }

            return null;
        }

        private static bool IsSupportedCollectionInterface(IExtendedType type) =>
            IsSupportedCollectionInterface(type, false);

        private static bool IsSupportedCollectionInterface(
            IExtendedType type,
            bool allowEnumerable)
        {
            if (type.TypeArguments.Count == 1)
            {
                if (type.Definition == typeof(IReadOnlyCollection<>)
                    || type.Definition == typeof(IReadOnlyList<>)
                    || type.Definition == typeof(ICollection<>)
                    || type.Definition == typeof(IList<>)
                    || type.Definition == typeof(IQueryable<>))
                {
                    return true;
                }

                if (allowEnumerable && type.Definition == typeof(IEnumerable<>))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsListType(IExtendedType type)
        {
            return type.IsArray
                || ImplementsListInterface(type);
        }

        private static bool IsTaskType(IExtendedType type)
        {
            return type.IsGeneric && typeof(Task<>) == type.Definition;
        }

        private static bool IsResolverResultType(IExtendedType type)
        {
            return type.IsGeneric
                && (typeof(IResolverResult<>) == type.Definition
                || typeof(ResolverResult<>) == type.Definition);
        }

        private static bool IsNonNullType(IExtendedType type)
        {
            return type.IsGeneric
                && typeof(NonNullType<>) == type.Definition;
        }

        private static bool IsPossibleNamedType(IExtendedType type)
        {
            return !IsTaskType(type)
                && !IsListType(type);
        }

        private static bool ImplementsListInterface(IExtendedType type)
        {
            return GetElementType(type) != null;
        }

        private static bool CanHandle(IExtendedType type)
        {
            return type.Kind == ExtendedTypeKind.Extended;
        }
    }
}
