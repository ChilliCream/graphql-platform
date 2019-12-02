using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    internal static class ExtendedTypeRewriter
    {
        public static Type Rewrite(IExtendedType type, params Nullable[] nullable)
        {
            var components = new Stack<IExtendedType>();
            IExtendedType? current = type;
            int i = 0;

            do
            {
                if (!IsNonEssentialComponent(current))
                {
                    bool makeNullable = current.IsNullable;

                    if (nullable.Length < i)
                    {
                        Nullable value = nullable[i++];
                        if (value != Nullable.Undefined)
                        {
                            makeNullable = value == Nullable.Yes;
                        }
                    }

                    if (current.IsNullable == makeNullable)
                    {
                        components.Push(current);
                    }
                    else
                    {
                        components.Push(new ExtendedType(
                            current.Type, makeNullable,
                            current.Kind, current.TypeArguments));
                    }
                }
                current = GetInnerType(current);
            } while (current != null && components.Count < 7);

            current = null;
            Type? rewritten = null;

            while (components.Count > 0)
            {
                if (rewritten is null)
                {
                    current = components.Pop();
                    rewritten = Rewrite(current.Type, current.IsNullable);
                }
                else
                {
                    current = components.Pop();
                    rewritten = current.IsArray && !typeof(IType).IsAssignableFrom(rewritten)
                        ? rewritten.MakeArrayType()
                        : MakeListType(rewritten);
                    rewritten = Rewrite(rewritten, current.IsNullable);
                }
            }

            return rewritten!;
        }

        private static Type Rewrite(Type type, bool isNullable)
        {
            if (type.IsValueType)
            {
                if (isNullable)
                {
                    return typeof(Nullable<>).MakeGenericType(type);
                }
                else
                {
                    return type;
                }
            }
            else
            {
                if (isNullable)
                {
                    return type;
                }
                else
                {
                    return MakeNonNullType(type);
                }
            }
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


        private static bool IsNonEssentialComponent(IExtendedType type)
        {
            return IsTaskType(type) || IsResolverResultType(type);
        }

        public static IExtendedType? GetInnerType(IExtendedType type)
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

        private static bool ImplementsListInterface(IExtendedType type)
        {
            return GetElementType(type) != null;
        }
    }
}
