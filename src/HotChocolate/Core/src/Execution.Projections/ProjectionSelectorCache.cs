using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Projections;

/// <summary>
/// Caches selector expressions per selection and include flags.
/// </summary>
internal sealed class ProjectionSelectorCache
{
    public const int DefaultCapacity = 4096;

    private readonly Cache<SelectorCacheKey, SelectorExpression> _cache;

    public ProjectionSelectorCache(
        int capacity = DefaultCapacity,
        CacheDiagnostics? diagnostics = null)
    {
        _cache = new Cache<SelectorCacheKey, SelectorExpression>(capacity, diagnostics);
    }

    internal SelectorExpression<TValue> GetOrCreate<TValue>(
        Selection selection,
        ulong includeFlags,
        Func<Selection, ulong, SelectorExpression<TValue>> create)
    {
        var key = new SelectorCacheKey(
            selection.DeclaringOperation.CacheId,
            selection.Id,
            includeFlags,
            null,
            typeof(TValue));

        return (SelectorExpression<TValue>)_cache.GetOrCreate(
            key,
            static (_, state) => state.Create(state.Selection, state.IncludeFlags),
            new SelectorCacheCreateState<TValue>(selection, includeFlags, create));
    }

    internal SelectorExpression<TValue> GetOrCreate<TValue>(
        Selection selection,
        ConditionFlags includeFlags,
        Func<Selection, ConditionFlags, SelectorExpression<TValue>> create)
    {
        var key = new SelectorCacheKey(
            selection.DeclaringOperation.CacheId,
            selection.Id,
            includeFlags.Word0,
            includeFlags.Overflow,
            typeof(TValue));

        return (SelectorExpression<TValue>)_cache.GetOrCreate(
            key,
            static (_, state) => state.Create(state.Selection, state.IncludeFlags),
            new WideSelectorCacheCreateState<TValue>(selection, includeFlags, create));
    }

    private readonly struct SelectorCacheKey(
        long operationId,
        int selectionId,
        ulong includeFlags,
        ulong[]? wideIncludeFlags,
        Type valueType) : IEquatable<SelectorCacheKey>
    {
        private readonly long _operationId = operationId;
        private readonly int _selectionId = selectionId;
        private readonly ulong _includeFlags = includeFlags;
        private readonly ulong[]? _wideIncludeFlags = wideIncludeFlags;
        private readonly Type _valueType = valueType;

        public bool Equals(SelectorCacheKey other)
            => _operationId == other._operationId
                && _selectionId == other._selectionId
                && _includeFlags == other._includeFlags
                && _valueType == other._valueType
                && _wideIncludeFlags.AsSpan().SequenceEqual(other._wideIncludeFlags);

        public override bool Equals(object? obj)
            => obj is SelectorCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_operationId);
            hash.Add(_selectionId);
            hash.Add(_includeFlags);
            hash.Add(_valueType);

            foreach (var includeFlag in _wideIncludeFlags.AsSpan())
            {
                hash.Add(includeFlag);
            }

            return hash.ToHashCode();
        }
    }

    private readonly record struct SelectorCacheCreateState<TValue>(
        Selection Selection,
        ulong IncludeFlags,
        Func<Selection, ulong, SelectorExpression<TValue>> Create);

    private readonly record struct WideSelectorCacheCreateState<TValue>(
        Selection Selection,
        ConditionFlags IncludeFlags,
        Func<Selection, ConditionFlags, SelectorExpression<TValue>> Create);
}
