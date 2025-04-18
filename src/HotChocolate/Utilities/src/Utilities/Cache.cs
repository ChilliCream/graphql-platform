using System.Collections.Concurrent;

namespace HotChocolate.Utilities;

/// <summary>
/// The core cache implementation for the HotChocolate library.
/// This cache uses a ring buffer to evict the least recently
/// used entries when the cache is full.
/// https://en.wikipedia.org/wiki/Page_replacement_algorithm#Clock
/// </summary>
/// <typeparam name="TValue"></typeparam>
public sealed class Cache<TValue>
{
    private readonly int _capacity;
    private readonly CacheEntry?[] _ring;
    private readonly ConcurrentDictionary<string, CacheEntry> _map;

    // the clock hand is incremented atomically and is used to
    // determine which cache entry to try to set a new entry into.
    private uint _hand;

    /// <summary>
    /// Creates a new instance of the <see cref="Cache{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of items that can be stored in this cache.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the capacity is less than 10.
    /// </exception>
    public Cache(int capacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 10);
        _capacity = capacity;
        _ring = new CacheEntry[capacity];
        _map = new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);
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
    public bool TryGet(string key, out TValue? value)
    {
        if (_map.TryGetValue(key, out var entry))
        {
            // we mark our entry as used by setting Accessed to 1
            // this means the entry will be safe from the next eviction.
            Volatile.Write(ref entry.Accessed, 1);
            value = entry.Value;
            return true;
        }

        value = default;
        return false;
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
        var entry = _map.GetOrAdd(
            key,
            static (k, arg) =>
            {
                var value = arg.create(k, arg.state);
                return arg.cache.InsertNew(k, value);
            },
            (state, create, cache: this));

        // in the case we did not add a new entry but instead retrieved it
        // from the ConcurrentDictionary we need to mark it as recently accessed
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

            if (entry is null)
            {
                // if the current cache slot is empty, we will try to insert
                // our entry into it with an atomic compare and swap.
                if (Interlocked.CompareExchange(ref _ring[idx], newEntry, null) is null)
                {
                    return newEntry;
                }

                continue;
            }

            if (Interlocked.CompareExchange(ref entry.Accessed, 0, 1) == 0)
            {
                // If we found a slot that was not recently retrieved, we will try to
                // replace it with our new entry. This will only succeed if no other thread
                // was able to replace the entry in the meantime. This operation is atomic.
                var prev = Interlocked.CompareExchange(ref _ring[idx], newEntry, entry);

                // If we were successful in replacing the entry, we will
                // then prev is the old entry and we need to remove it from the map.
                // It might be that the old entry was retrieved in the meantime, and we
                // accept this small window in which the map might have a dangling reference.
                if (ReferenceEquals(prev, entry))
                {
                    _map.TryRemove(prev.Key, out _);
                    return newEntry;
                }
            }

            if (++spins > maxSpins)
            {
                entry = _ring[idx]!; // re‑read reference

                var oldKey = entry.Key;

                // atomic swap
                Interlocked.Exchange(ref _ring[idx], newEntry);

                _map.TryRemove(oldKey, out _);
                return newEntry;
            }
        }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        _map.Clear();

        for (var i = 0; i < _ring.Length; i++)
        {
            _ring[i] = null;
        }

        Interlocked.Exchange(ref _hand, 0);
    }

    private sealed class CacheEntry(string key, TValue value)
    {
        public readonly string Key = key;

        public readonly TValue Value = value;

        // 0 = not accessed recently
        // 1 = accessed recently
        public int Accessed = 1;
    }
}
