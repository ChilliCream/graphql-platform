using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Caching.Memory;

/// <summary>
/// The core cache implementation for the HotChocolate library.
/// This cache uses a ring buffer to evict the least recently
/// used entries when the cache is full.
/// https://en.wikipedia.org/wiki/Page_replacement_algorithm#Clock
/// </summary>
/// <typeparam name="TValue">
/// The type of the value that is stored in the cache.
/// </typeparam>
public sealed class Cache<TValue>
{
    private readonly int _capacity;
    private readonly CacheEntry?[] _ring;
    private readonly ConcurrentDictionary<string, CacheEntry> _map;
    private readonly CacheDiagnostics _diagnostics;

    // The clock hand is incremented atomically and is used to
    // determine which cache entry to try to set a new entry into.
    // We start with uint.MaxValue to ensure that we start with slot 0.
    private uint _hand = uint.MaxValue;

    /// <summary>
    /// Creates a new instance of the <see cref="Cache{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of items that can be stored in this cache.
    /// </param>
    /// <param name="diagnostics">
    /// The diagnostics for the cache.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the capacity is less than 10.
    /// </exception>
    public Cache(int capacity = 256, CacheDiagnostics? diagnostics = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _capacity = capacity;
        _ring = new CacheEntry[capacity];
        _map = new ConcurrentDictionary<string, CacheEntry>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: _capacity,
            comparer: StringComparer.Ordinal);
        _diagnostics = diagnostics ?? NoOpCacheDiagnostics.Instance;
        _diagnostics.RegisterCapacityGauge(() => _capacity);
        _diagnostics.RegisterSizeGauge(() => _map.Count);
    }

    /// <summary>
    /// Gets the maximum allowed item count that can be stored in this cache.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the item count currently stored in this cache.
    /// This count may be temporarily larger than the allowed capacity.
    /// </summary>
    public int Count => _map.Count;

    /// <summary>
    /// Tries to get a value from the cache.
    /// </summary>
    /// <param name="key">
    /// The key to look up.
    /// </param>
    /// <param name="value">
    /// The value that was found.
    /// </param>
    /// <returns>
    /// True if the value was found, otherwise false.
    /// </returns>
    public bool TryGet(string key, [NotNullWhen(true)] out TValue? value)
    {
        if (_map.TryGetValue(key, out var entry))
        {
            // We mark our entry as used by setting Accessed to 1
            // this means the entry will be safe from the next eviction.
            // Note: Volatile.Write is faster than Interlocked.Exchange, and we accept the
            // tiny risk that an in‑flight eviction may still remove this entry.
            Volatile.Write(ref entry.Accessed, 1);
            _diagnostics.Hit();
            value = entry.Value!;
            return true;
        }

        _diagnostics.Miss();
        value = default;
        return false;
    }

    /// <summary>
    /// Tries to add a value to the cache if it does not exist.
    /// </summary>
    /// <param name="key">
    /// The key to add.
    /// </param>
    /// <param name="value">
    /// The value to add.
    /// </param>
    public void TryAdd(string key, TValue value)
    {
        var args = new CacheEntryCreateArgs<TValue>(value, static (_, v) => v, this);

        // we use the same mechanism as in GetOrCreate, but we do not
        // do the extra lookup.
        _map.GetOrAdd(
            key,
            static (k, arg) =>
            {
                arg.Diagnostics.Miss();
                var value = arg.Create(k, arg.State);
                return arg.Cache.InsertNew(k, value);
            },
            args);
    }

    /// <summary>
    /// Gets a value from the cache or creates it if it does not exist.
    /// </summary>
    /// <param name="key">
    /// The key to look up.
    /// </param>
    /// <param name="create">
    /// The function to create the value if it does not exist.
    /// </param>
    /// <returns>
    /// The value that was found or created.
    /// </returns>
    public TValue GetOrCreate(string key, Func<string, TValue> create)
        => GetOrCreate(key, static (k, f) => f(k), create);

    /// <summary>
    /// Gets a value from the cache or creates it if it does not exist.
    /// </summary>
    /// <param name="key">
    /// The key to look up.
    /// </param>
    /// <param name="create">
    /// The function to create the value if it does not exist.
    /// </param>
    /// <param name="state">
    /// The state that is passed to the create function.
    /// </param>
    /// <typeparam name="TState">
    /// The type of the state that is passed to the create function.
    /// </typeparam>
    /// <returns>
    /// The value that was found or created.
    /// </returns>
    public TValue GetOrCreate<TState>(string key, Func<string, TState, TValue> create, TState state)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(create);

        // We first check if the entry is already in the map.
        // This is a fast lookup and will be used most of the time.
        if (_map.TryGetValue(key, out var entry))
        {
            // We mark our entry as used by setting Accessed to 1
            // this means the entry will be safe from the next eviction.
            // Note: Volatile.Write is faster than Interlocked.Exchange, and we accept the
            // tiny risk that an in‑flight eviction may still remove this entry.
            Volatile.Write(ref entry.Accessed, 1);
            _diagnostics.Hit();
            return entry.Value;
        }

        // If we have miss, we do a GetOrAdd on the map to get at the end
        // the winner in case of contention.
        //
        // The GetOrAdd of the ConcurrentDictionary is not atomic.
        // It is possible that two threads will try to create the same entry
        // at the same time.
        // In this case the orphaned entry will hang around in the ring and may:
        // - Skew eviction behavior slightly.
        // - Evict useful entries unnecessarily.
        // - Waste ring capacity.
        //
        // But unless the cache capacity is very extremely small or
        // contention is very high, this is acceptable as we will
        // eventually evict the orphaned entry and also can avoid
        // the overhead of a lock on the dictionary itself.
        // The ConcurrentDictionary is vert efficient and does not
        // lock the whole dictionary when adding an entry.
        var args = new CacheEntryCreateArgs<TState>(state, create, this);

        entry = _map.GetOrAdd(
            key,
            static (k, arg) =>
            {
                arg.Diagnostics.Miss();
                var value = arg.Create(k, arg.State);
                return arg.Cache.InsertNew(k, value);
            },
            args);

        // In the case we did not add a new entry but instead retrieved it
        // from the ConcurrentDictionary we need to mark it as recently accessed
        // Note: Volatile.Write is faster than Interlocked.Exchange, and we accept the
        // tiny risk that an in‑flight eviction may still remove this entry.
        Volatile.Write(ref entry.Accessed, 1);
        return entry.Value;
    }

    private CacheEntry InsertNew(string key, TValue value)
    {
        var maxSpins = _capacity * 2;
        var spins = 0;
        var newEntry = new CacheEntry(key, value);

        while (true)
        {
            var handle = Interlocked.Increment(ref _hand);
            var idx = (int)(handle % (uint)_capacity);
            var entry = _ring[idx];

            if (++spins > maxSpins && entry is not null)
            {
                var prev = Interlocked.CompareExchange(ref _ring[idx], newEntry, entry);
                if (ReferenceEquals(prev, entry))
                {
                    _map.TryRemove(prev.Key, out _);
                    _diagnostics.Evict();
                    return newEntry;
                }
            }

            if (entry is null)
            {
                // if the current cache slot is empty, we will try to insert
                // our entry into it with an atomic compare and swap.
                if (Interlocked.CompareExchange(ref _ring[idx], newEntry, null) is null)
                {
                    return newEntry;
                }
            }
            else if (Interlocked.CompareExchange(ref entry.Accessed, 0, 1) == 0)
            {
                // If we found a slot that was not recently retrieved, we will try to
                // replace it with our new entry. This will only succeed if no other thread
                // was able to replace the entry in the meantime. This operation is atomic.
                var prev = Interlocked.CompareExchange(ref _ring[idx], newEntry, entry);

                // If we were successful in replacing the entry, we will
                // then prev is the old entry, and we need to remove it from the map.
                // It might be that the old entry was retrieved in the meantime, and we
                // accept this small window in which the map might have a dangling reference.
                if (ReferenceEquals(prev, entry))
                {
                    _map.TryRemove(prev.Key, out _);
                    _diagnostics.Evict();
                    return newEntry;
                }
            }
        }
    }

    /// <summary>
    /// Returns all keys in the cache. This method is for testing only.
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<string> GetKeys()
    {
        foreach (var entry in _ring)
        {
            if (entry is not null)
            {
                yield return entry.Key;
            }
        }
    }

    private sealed class CacheEntry(string key, TValue value)
    {
        /// <summary>
        /// The key of the entry.
        /// </summary>
        public readonly string Key = key;

        /// <summary>
        /// The value of the entry.
        /// </summary>
        public readonly TValue Value = value;

        /// <summary>
        /// 0 = not accessed recently
        /// 1 = accessed recently
        /// </summary>
        public int Accessed = 1;
    }

    private readonly struct CacheEntryCreateArgs<TState>(
        TState state,
        Func<string, TState, TValue> create,
        Cache<TValue> cache)
    {
        /// <summary>
        /// The state that is needed to create the value to cache.
        /// </summary>
        public readonly TState State = state;

        /// <summary>
        /// The factory to create the value to cache.
        /// </summary>
        public readonly Func<string, TState, TValue> Create = create;

        /// <summary>
        /// The cache instance.
        /// </summary>
        public readonly Cache<TValue> Cache = cache;

        /// <summary>
        /// The diagnostics for the cache.
        /// </summary>
        public readonly CacheDiagnostics Diagnostics = cache._diagnostics;
    }
}
