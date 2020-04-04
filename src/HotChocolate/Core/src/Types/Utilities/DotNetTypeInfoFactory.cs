using System;
using System.Collections.Generic;
using System.Linq;
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

                if (components.Count > 0 &&
                    TryCreateBluePrint(components, 0, out IType bluePrint) &&
                    TypeFactoryHelper.IsTypeStructureValid(bluePrint))
                {
                    typeInfo = new TypeInfo(
                        components.Last(),
                        components,
                        n => bluePrint.Rewrite(n));
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

        public static Type UnwrapNonNull(Type type)
        {
            if (IsNonNullType(type))
            {
                return GetInnerType(type);
            }
            return type;
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

        private static Type MakeNonNullType(Type nullableType)
        {
            Type wrapper = typeof(NativeType<>).MakeGenericType(nullableType);
            return typeof(NonNullType<>).MakeGenericType(wrapper);
        }

        private static bool TryCreateBluePrint(
            List<Type> components,
            int index,
            out IType bluePrint)
        {
            if (index < components.Count)
            {
                Type component = components[index];

                if (index == components.Count - 1)
                {
                    if (IsPossibleNamedType(component))
                    {
                        bluePrint = component.IsValueType
                            ? (IType)new NonNullType(TypeFactoryHelper.PlaceHolder)
                            : TypeFactoryHelper.PlaceHolder;
                        return true;
                    }
                }
                else if (IsNullableType(component))
                {
                    if (TryCreateBluePrint(components, index + 1, out IType innerType))
                    {
                        bluePrint = innerType.Kind == TypeKind.NonNull
                            ? innerType.InnerType()
                            : innerType;
                        return true;
                    }
                }
                else if (IsNonNullType(component))
                {
                    if (TryCreateBluePrint(components, index + 1, out IType innerType))
                    {
                        bluePrint = innerType.Kind == TypeKind.NonNull
                            ? innerType
                            : new NonNullType(innerType);
                        return true;
                    }
                }
                else if (IsNullableType(component))
                {
                    if (TryCreateBluePrint(components, index + 1, out IType innerType))
                    {
                        bluePrint = innerType;
                        return true;
                    }
                }
                else if (IsListType(component))
                {
                    if (TryCreateBluePrint(components, index + 1, out IType innerType))
                    {
                        bluePrint = new ListType(innerType);
                        return true;
                    }
                }
            }

            bluePrint = null;
            return false;
        }

        private static List<Type> DecomposeType(Type type)
        {
            var components = new List<Type>();
            Type current = type;

            do
            {
                current = RemoveNonEssentialParts(current);
                if (components.Count == 0
                    || !IsNonNullType(components[components.Count - 1])
                    || !IsNullableType(current))
                {
                    components.Add(current);
                }
                current = GetInnerType(current);
            } while (current != null && components.Count < 5);

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

            if (IsResolverResultType(current))
            {
                current = GetInnerType(current);
            }

            if (IsOptional(type))
            {
                current = GetInnerType(current);
            }

            return current;
        }

        private static bool IsTypeStackValid(List<Type> components)
        {
            if (components.Count == 0)
            {
                return false;
            }

            foreach (Type type in components)
            {
                if (typeof(Task).IsAssignableFrom(type))
                {
                    return false;
                }
            }

            if (typeof(IType).IsAssignableFrom(components[components.Count - 1]))
            {
                return false;
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
                || IsNonNullType(type)
                || IsNullableType(type)
                || IsWrapperType(type)
                || IsResolverResultType(type)
                || IsOptional(type))
            {
                return type.GetGenericArguments().First();
            }

            if (ImplementsListInterface(type))
            {
                return GetInnerListType(type);
            }

            return null;
        }

        internal static Type GetInnerListType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListType<>))
            {
                return type.GetGenericArguments().First();
            }

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
                    || typeDefinition == typeof(IList<>)
                    || typeDefinition == typeof(IQueryable<>)
                    || typeDefinition == typeof(IAsyncEnumerable<>)
                    || typeDefinition == typeof(IObservable<>))
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

        public static bool IsListType(Type type)
        {
            return type.IsArray
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListType<>))
                || ImplementsListInterface(type);
        }

        private static bool IsTaskType(Type type)
        {
            return type.IsGenericType
                && (typeof(Task<>) == type.GetGenericTypeDefinition()
                    || typeof(ValueTask<>) == type.GetGenericTypeDefinition());
        }

        private static bool IsOptional(Type type)
        {
            return type.IsGenericType
                && typeof(Optional<>) == type.GetGenericTypeDefinition();
        }

        private static bool IsResolverResultType(Type type)
        {
            return type.IsGenericType
                && (typeof(IResolverResult<>) == type.GetGenericTypeDefinition()
                || typeof(ResolverResult<>) == type.GetGenericTypeDefinition());
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType
                && typeof(Nullable<>) == type.GetGenericTypeDefinition();
        }

        private static bool IsNonNullType(Type type)
        {
            return type.IsGenericType
                && typeof(NonNullType<>) == type.GetGenericTypeDefinition();
        }

        private static bool IsPossibleNamedType(Type type)
        {
            return !IsNullableType(type)
                && !IsTaskType(type)
                && !IsListType(type)
                && !IsWrapperType(type);
        }

        private static bool ImplementsListInterface(Type type)
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
            if (!typeof(IType).IsAssignableFrom(type) || IsWrapperType(type))
            {
                return true;
            }

            if (IsNonNullType(type) && CanHandle(GetInnerType(type)))
            {
                return true;
            }

            List<Type> types = DecomposeType(type);
            if (types.Count > 0 && CanHandle(types[types.Count - 1]))
            {
                return true;
            }

            return false;
        }
    }
}
