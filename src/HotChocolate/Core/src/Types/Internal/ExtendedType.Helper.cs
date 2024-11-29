using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class ExtendedType
{
    private static class Helper
    {
        internal static bool IsSchemaType(Type type)
        {
            if (BaseTypes.IsNamedType(type))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (typeof(ListType<>) == definition ||
                    typeof(NonNullType<>) == definition ||
                    typeof(NativeType<>) == definition)
                {
                    return IsSchemaType(type.GetGenericArguments()[0]);
                }
            }

            return false;
        }

        internal static Type RemoveNonEssentialTypes(Type type)
        {
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(NativeType<>) ||
                type.GetGenericTypeDefinition() == typeof(ValueTask<>) ||
                type.GetGenericTypeDefinition() == typeof(Task<>)))
            {
                return RemoveNonEssentialTypes(type.GetGenericArguments()[0]);
            }
            return type;
        }

        internal static IExtendedType RemoveNonEssentialTypes(IExtendedType type)
        {
            if (type.IsGeneric &&
                (type.Definition == typeof(NativeType<>) ||
                type.Definition == typeof(ValueTask<>) ||
                type.Definition == typeof(Task<>)))
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

        internal static ExtendedType? GetInnerListType(
            ExtendedType type,
            TypeCache cache)
        {
            if (IsDictionary(type.Type))
            {
                IExtendedType key = type.TypeArguments[0];
                IExtendedType value = type.TypeArguments[1];

                var itemType = typeof(KeyValuePair<,>).MakeGenericType(key.Type, value.Type);

                return cache.GetOrCreateType(itemType, () =>
                {
                    return new ExtendedType(
                        itemType,
                        ExtendedTypeKind.Runtime,
                        new[] { (ExtendedType)key, (ExtendedType)value, },
                        isNullable: false);
                });
            }

            if (IsSupportedCollectionInterface(type.Type))
            {
                return type.TypeArguments[0];
            }

            if (type.IsInterface && type.Definition == typeof(IEnumerable<>))
            {
                return type.TypeArguments[0];
            }

            foreach (var interfaceType in type.Type.GetInterfaces())
            {
                if (IsSupportedCollectionInterface(interfaceType))
                {
                    var elementType = interfaceType.GetGenericArguments()[0];

                    if (type.TypeArguments.Count == 1 &&
                        type.TypeArguments[0].Type == elementType)
                    {
                        return type.TypeArguments[0];
                    }

                    return SystemType.FromType(elementType, cache);
                }
            }

            return null;
        }

        internal static Type? GetInnerListType(Type type)
        {
            var typeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

            if (IsSupportedCollectionInterface(type))
            {
                return type.GetGenericArguments()[0];
            }

            if (type.IsInterface && typeDefinition == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (IsSupportedCollectionInterface(interfaceType))
                {
                    return interfaceType.GetGenericArguments()[0];
                }

                if (interfaceType == typeof(IPage) &&
                    type.IsGenericType &&
                    type.GenericTypeArguments.Length == 1)
                {
                    return type.GenericTypeArguments[0];
                }
            }

            return null;
        }

        public static bool IsSupportedCollectionInterface(Type type)
        {
            if (type.IsGenericType)
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IReadOnlyCollection<>)
                    || typeDefinition == typeof(IReadOnlyList<>)
                    || typeDefinition == typeof(ICollection<>)
                    || typeDefinition == typeof(IList<>)
                    || typeDefinition == typeof(ISet<>)
                    || typeDefinition == typeof(IQueryable<>)
                    || typeDefinition == typeof(IAsyncEnumerable<>)
                    || typeDefinition == typeof(IObservable<>)
                    || typeDefinition == typeof(List<>)
                    || typeDefinition == typeof(Collection<>)
                    || typeDefinition == typeof(Stack<>)
                    || typeDefinition == typeof(HashSet<>)
                    || typeDefinition == typeof(Queue<>)
                    || typeDefinition == typeof(ConcurrentBag<>)
                    || typeDefinition == typeof(ImmutableArray<>)
                    || typeDefinition == typeof(ImmutableList<>)
                    || typeDefinition == typeof(ImmutableQueue<>)
                    || typeDefinition == typeof(ImmutableStack<>)
                    || typeDefinition == typeof(ImmutableHashSet<>)
                    || typeDefinition == typeof(ISourceStream<>)
                    || typeDefinition == typeof(IExecutable<>))
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
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IDictionary<,>)
                    || typeDefinition == typeof(IReadOnlyDictionary<,>)
                    || typeDefinition == typeof(Dictionary<,>)
                    || typeDefinition == typeof(ConcurrentDictionary<,>)
                    || typeDefinition == typeof(SortedDictionary<,>)
                    || typeDefinition == typeof(ImmutableDictionary<,>))
                {
                    return true;
                }
            }
            return false;
        }

        internal static IExtendedType ChangeNullability(
            IExtendedType type,
            ReadOnlySpan<bool?> nullable,
            TypeCache cache)
        {
            if (nullable.Length == 0)
            {
                return type;
            }

            var id = Tools.CreateId(type, nullable);

            if (cache.TryGetType(id, out var extendedType))
            {
                return extendedType;
            }

            var pos = 0;
            return ChangeNullability(id, (ExtendedType)type, nullable, ref pos, cache);
        }

        private static ExtendedType ChangeNullability(
            ExtendedTypeId id,
            ExtendedType type,
            ReadOnlySpan<bool?> nullable,
            ref int position,
            TypeCache cache)
        {
            if (cache.TryGetType(id, out var cached))
            {
                return cached;
            }

            var pos = position++;
            var changeNullability =
                nullable.Length > pos &&
                nullable[pos].HasValue &&
                nullable[pos]!.Value != type.IsNullable;
            var typeArguments = type.TypeArguments;

            if (type.TypeArguments.Count > 0 && nullable.Length > position)
            {
                var args = new ExtendedType[type.TypeArguments.Count];

                for (var j = 0; j < type.TypeArguments.Count; j++)
                {
                    var typeArgument = type.TypeArguments[j];
                    var typeArgumentId =
                        Tools.CreateId(typeArgument, nullable.Slice(position));

                    args[j] = nullable.Length > position
                        ? ChangeNullability(
                            typeArgumentId,
                            typeArgument,
                            nullable,
                            ref position,
                            cache)
                        : type.TypeArguments[j];
                }

                typeArguments = args;
            }

            if (changeNullability || !ReferenceEquals(typeArguments, type.TypeArguments))
            {
                var elementType = type.IsArrayOrList
                    ? type.ElementType
                    : null;

                if (elementType is not null &&
                    !ReferenceEquals(typeArguments, type.TypeArguments))
                {
                    for (var e = 0; e < type.TypeArguments.Count; e++)
                    {
                        if (ReferenceEquals(elementType, type.TypeArguments[e]))
                        {
                            elementType = typeArguments[e];
                        }
                    }
                }

                var rewritten = new ExtendedType(
                    type.Type,
                    type.Kind,
                    typeArguments: typeArguments,
                    source: type.Source,
                    definition: type.Definition,
                    elementType: elementType,
                    isList: type.IsList,
                    isNamedType: type.IsNamedType,
                    isNullable: nullable[pos] ?? type.IsNullable);

                return cache.TryAdd(rewritten)
                    ? rewritten
                    : cache.GetType(rewritten.Id);
            }

            return type;
        }

        internal static ExtendedTypeId CreateIdentifier(IExtendedType type)
        {
            var position = 0;
            Span<bool> nullability = stackalloc bool[32];
            CollectNullability(type, nullability, ref position);

            return CreateIdentifier(
                type.Source,
                type.Kind,
                nullability.Slice(0, position));
        }

        internal static ExtendedTypeId CreateIdentifier(
            IExtendedType type,
            ReadOnlySpan<bool?> nullabilityChange)
        {
            var position = 0;
            Span<bool> nullability = stackalloc bool[32];
            CollectNullability(type, nullability, ref position);
            nullability = nullability.Slice(0, position);

            var length = nullability.Length < nullabilityChange.Length
                ? nullability.Length
                : nullabilityChange.Length;

            for (var i = 0; i < length; i++)
            {
                var change = nullabilityChange[i];
                if (change.HasValue)
                {
                    nullability[i] = change.Value;
                }
            }

            return CreateIdentifier(
                type.Source,
                type.Kind,
                nullability);
        }

        private static ExtendedTypeId CreateIdentifier(
            Type source,
            ExtendedTypeKind kind,
            ReadOnlySpan<bool> nullability)
        {
            return new ExtendedTypeId(
                source,
                kind,
                CompactNullability(nullability));
        }

        internal static void CollectNullability(
            IExtendedType type,
            Span<bool> nullability,
            ref int position)
        {
            if (position >= 32)
            {
                throw new NotSupportedException(
                    "Types with more than 32 components are not allowed.");
            }

            nullability[position++] = type.IsNullable;

            foreach (var typeArgument in type.TypeArguments)
            {
                CollectNullability(typeArgument, nullability, ref position);
            }
        }

        private static uint CompactNullability(ReadOnlySpan<bool> bits)
        {
            uint nullability = 0;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    nullability |= 1u << (bits.Length - i);
                }
            }
            return nullability;
        }
    }
}
