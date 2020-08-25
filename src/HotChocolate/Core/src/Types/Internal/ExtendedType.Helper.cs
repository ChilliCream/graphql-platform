using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
    {
        private static class Helper
        {
            internal static bool IsSchemaType(Type type)
            {
                if (BaseTypes.IsGenericBaseType(type))
                {
                    return true;
                }

                if (type.IsGenericType)
                {
                    Type definition = type.GetGenericTypeDefinition();
                    if (typeof(ListType<>) == definition
                        || typeof(NonNullType<>) == definition
                        || typeof(NativeType<>) == definition)
                    {
                        return IsSchemaType(type.GetGenericArguments()[0]);
                    }
                }

                return false;
            }

            internal static Type RemoveNonEssentialTypes(Type type)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NativeType<>))
                {
                    return RemoveNonEssentialTypes(type.GetGenericArguments()[0]);
                }
                return type;
            }

            internal static IExtendedType RemoveNonEssentialTypes(IExtendedType type)
            {
                if (type.IsGeneric && type.Definition == typeof(NativeType<>))
                {
                    return RemoveNonEssentialTypes(type.TypeArguments[0]);
                }
                return type;
            }

            internal static bool IsListType(Type type) =>
                type.IsArray
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListType<>))
                || ImplementsListInterface(type);

            private static bool ImplementsListInterface(Type type) =>
                GetInnerListType(type) is not null;

            /*
            private static IExtendedType? GetInnerListType(IExtendedType type)
            {
                if (type.Definition == typeof(ListType<>))
                {
                    return type.TypeArguments[0];
                }

                if (IsDictionary(type.Type))
                {
                    IExtendedType key = type.TypeArguments[0];
                    IExtendedType value = type.TypeArguments[1];

                    return new ExtendedType2(
                        typeof(KeyValuePair<,>).MakeGenericType(key.Type, value.Type),
                        ExtendedTypeKind.Runtime,
                        new [] { (ExtendedType2)key, (ExtendedType2)value },
                        isNullable: false);
                }

                if (IsSupportedCollectionInterface(type.Type))
                {
                    return type.TypeArguments[0];
                }

                if (type.IsInterface && type.Definition == typeof(IEnumerable<>))
                {
                    return type.TypeArguments[0];
                }

                foreach (Type interfaceType in type.Type.GetInterfaces())
                {
                    if (IsSupportedCollectionInterface(interfaceType))
                    {
                        Type elementType = interfaceType.GetGenericArguments()[0];

                        if (type.TypeArguments.Count == 1 &&
                            type.TypeArguments[0].Type == elementType)
                        {
                            return type.TypeArguments[0];
                        }

                        return SystemType.FromType (elementType);
                    }
                }

                return null;
            }
            */

            internal static Type? GetInnerListType(Type type)
            {
                Type? typeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

                if (typeDefinition == typeof(ListType<>))
                {
                    return type.GetGenericArguments()[0];
                }

                if (IsSupportedCollectionInterface(type))
                {
                    return type.GetGenericArguments()[0];
                }

                if (type.IsInterface && typeDefinition == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }

                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (IsSupportedCollectionInterface(interfaceType))
                    {
                        return interfaceType.GetGenericArguments()[0];
                    }
                }

                return null;
            }

            public static bool IsSupportedCollectionInterface(Type type)
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
                        || typeDefinition == typeof(IObservable<>)
                        || typeDefinition == typeof(List<>)
                        || typeDefinition == typeof(Collection<>)
                        || typeDefinition == typeof(Stack<>)
                        || typeDefinition == typeof(Queue<>)
                        || typeDefinition == typeof(ConcurrentBag<>))
                    {
                        return true;
                    }
                }
                return false;
            }

            private static bool IsDictionary(Type type)
            {
                if (type.IsGenericType)
                {
                    Type typeDefinition = type.GetGenericTypeDefinition();
                    if (typeDefinition == typeof(IDictionary<,>)
                        || typeDefinition == typeof(IReadOnlyDictionary<,>)
                        || typeDefinition == typeof(Dictionary<,>)
                        || typeDefinition == typeof(ConcurrentDictionary<,>)
                        || typeDefinition == typeof(SortedDictionary<,>))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
