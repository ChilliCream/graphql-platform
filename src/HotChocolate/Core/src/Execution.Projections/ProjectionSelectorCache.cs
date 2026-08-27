using System.Collections.Concurrent;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Projections;

/// <summary>
/// Caches selector expressions per selection and include flags.
/// </summary>
internal sealed class ProjectionSelectorCache
{
    public const int DefaultCapacity = 4096;

    private readonly int _capacity;
    private readonly CacheEntry?[] _ring;
    private readonly ConcurrentDictionary<SelectorCacheKey, NarrowCacheEntry> _cache;
    private readonly CacheDiagnostics? _diagnostics;
    private ConcurrentDictionary<WideSelectorCacheKey, WideCacheEntry>? _wideCache;
    private uint _hand = uint.MaxValue;

    public ProjectionSelectorCache(
        int capacity = DefaultCapacity,
        CacheDiagnostics? diagnostics = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _capacity = capacity;
        _ring = new CacheEntry[capacity];
        _cache = new ConcurrentDictionary<SelectorCacheKey, NarrowCacheEntry>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: capacity);
        _diagnostics = diagnostics;
        _diagnostics?.RegisterCapacityGauge(() => _capacity);
        _diagnostics?.RegisterSizeGauge(() => _cache.Count + (_wideCache?.Count ?? 0));
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
            typeof(TValue));

        if (_cache.TryGetValue(key, out var entry))
        {
            Volatile.Write(ref entry.Accessed, 1);
            _diagnostics?.Hit();
            return (SelectorExpression<TValue>)entry.Value;
        }

        entry = _cache.GetOrAdd(
            key,
            static (key, state) => state.Cache.InsertNew(
                new NarrowCacheEntry(
                    key,
                    state.Create(state.Selection, state.IncludeFlags))),
            new SelectorCacheCreateState<TValue>(this, selection, includeFlags, create));

        Volatile.Write(ref entry.Accessed, 1);
        return (SelectorExpression<TValue>)entry.Value;
    }

    internal SelectorExpression<TValue> GetOrCreate<TValue>(
        Selection selection,
        ConditionFlags includeFlags,
        Func<Selection, ConditionFlags, SelectorExpression<TValue>> create)
    {
        var key = new WideSelectorCacheKey(
            selection.DeclaringOperation.CacheId,
            selection.Id,
            includeFlags,
            typeof(TValue));

        var cache = Volatile.Read(ref _wideCache);
        if (cache is null)
        {
            var newCache = new ConcurrentDictionary<WideSelectorCacheKey, WideCacheEntry>(
                concurrencyLevel: Environment.ProcessorCount,
                capacity: _capacity);
            cache = Interlocked.CompareExchange(ref _wideCache, newCache, null) ?? newCache;
        }

        if (cache.TryGetValue(key, out var entry))
        {
            Volatile.Write(ref entry.Accessed, 1);
            _diagnostics?.Hit();
            return (SelectorExpression<TValue>)entry.Value;
        }

        entry = cache.GetOrAdd(
            key,
            static (key, state) => state.Cache.InsertNew(
                new WideCacheEntry(
                    key,
                    state.Create(state.Selection, state.IncludeFlags))),
            new WideSelectorCacheCreateState<TValue>(this, selection, includeFlags, create));

        Volatile.Write(ref entry.Accessed, 1);
        return (SelectorExpression<TValue>)entry.Value;
    }

    private TEntry InsertNew<TEntry>(TEntry newEntry)
        where TEntry : CacheEntry
    {
        _diagnostics?.Miss();

        var maxSpins = _capacity * 2;
        var spins = 0;

        while (true)
        {
            var handle = Interlocked.Increment(ref _hand);
            var idx = (int)(handle % (uint)_capacity);
            var entry = _ring[idx];

            if (++spins > maxSpins && entry is not null)
            {
                var previous = Interlocked.CompareExchange(ref _ring[idx], newEntry, entry);
                if (ReferenceEquals(previous, entry))
                {
                    entry.Remove(this);
                    _diagnostics?.Evict();
                    return newEntry;
                }
            }

            if (entry is null)
            {
                if (Interlocked.CompareExchange(ref _ring[idx], newEntry, null) is null)
                {
                    return newEntry;
                }
            }
            else if (Interlocked.CompareExchange(ref entry.Accessed, 0, 1) == 0)
            {
                var previous = Interlocked.CompareExchange(ref _ring[idx], newEntry, entry);
                if (ReferenceEquals(previous, entry))
                {
                    entry.Remove(this);
                    _diagnostics?.Evict();
                    return newEntry;
                }
            }
        }
    }

    private readonly record struct SelectorCacheKey(
        long OperationId,
        int SelectionId,
        ulong IncludeFlags,
        Type ValueType);

    private readonly record struct SelectorCacheCreateState<TValue>(
        ProjectionSelectorCache Cache,
        Selection Selection,
        ulong IncludeFlags,
        Func<Selection, ulong, SelectorExpression<TValue>> Create);

    private readonly struct WideSelectorCacheKey(
        long operationId,
        int selectionId,
        ConditionFlags includeFlags,
        Type valueType) : IEquatable<WideSelectorCacheKey>
    {
        private readonly long _operationId = operationId;
        private readonly int _selectionId = selectionId;
        private readonly ulong _includeFlags = includeFlags.Word0;
        private readonly ulong[]? _wideIncludeFlags = includeFlags.Overflow;
        private readonly Type _valueType = valueType;

        public bool Equals(WideSelectorCacheKey other)
            => _operationId == other._operationId
                && _selectionId == other._selectionId
                && _includeFlags == other._includeFlags
                && _valueType == other._valueType
                && _wideIncludeFlags.AsSpan().SequenceEqual(other._wideIncludeFlags);

        public override bool Equals(object? obj)
            => obj is WideSelectorCacheKey other && Equals(other);

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

    private readonly record struct WideSelectorCacheCreateState<TValue>(
        ProjectionSelectorCache Cache,
        Selection Selection,
        ConditionFlags IncludeFlags,
        Func<Selection, ConditionFlags, SelectorExpression<TValue>> Create);

    private abstract class CacheEntry(SelectorExpression value)
    {
        public readonly SelectorExpression Value = value;

        public int Accessed = 1;

        public abstract void Remove(ProjectionSelectorCache cache);
    }

    private sealed class NarrowCacheEntry(SelectorCacheKey key, SelectorExpression value)
        : CacheEntry(value)
    {
        public override void Remove(ProjectionSelectorCache cache)
            => cache._cache.TryRemove(key, out _);
    }

    private sealed class WideCacheEntry(WideSelectorCacheKey key, SelectorExpression value)
        : CacheEntry(value)
    {
        public override void Remove(ProjectionSelectorCache cache)
            => cache._wideCache?.TryRemove(key, out _);
    }
}
