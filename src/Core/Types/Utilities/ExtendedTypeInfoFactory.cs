using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    internal class ExtendedTypeInfoFactory
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
                List<TypeComponent> components = DecomposeType(type);
                if (components.Count > 0)
                {
                    typeInfo = new TypeInfo(
                        components[components.Count - 1].Type,
                        components);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        public static IExtendedType Unwrap(IExtendedType type)
        {
            return RemoveNonEssentialParts(type);
        }

        public static IExtendedType Rewrite(
            IExtendedType type,
            ReadOnlySpan<Nullable> nullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (CanHandle(type))
            {
                var components = new Stack<IExtendedType>();
                IExtendedType? current = type;
                int i = 0;

                do
                {
                    bool shallBeNullable = nullable.Length < i && nullable[i++] == Nullable.Yes;
                    current = RemoveNonEssentialParts(current);

                    if (current.IsNullable == shallBeNullable)
                    {
                        components.Push(current);
                    }
                    else
                    {
                        components.Push(current.WithIsNullable(shallBeNullable));
                    }

                    current = GetInnerType(current);
                } while (current != null && components.Count < 7);

                current = null;

                while (components.Count > 0)
                {
                    if (current is null)
                    {
                        current = components.Pop();
                    }
                    else
                    {
                        var parent = components.Pop();
                        if (parent.TypeArguments.Contains(current))
                        {
                            current = parent;
                        }
                        else
                        {
                            current = parent.WithTypeArguments(new[] { current });
                        }
                    }
                }

                return current!;
            }

            return type;
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
                    current = GetInnerType(current);
                }
                else
                {
                    components.Add(new TypeComponent(
                        TypeComponentKind.Named,
                        current.Type));
                    current = null;
                }
            } while (current != null && components.Count < 7);

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
            return type.Kind == ExtendedTypeKind.Extended
                || type.Kind == ExtendedTypeKind.Unknown;
        }
    }
}
