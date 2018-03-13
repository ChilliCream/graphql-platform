using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Prometheus.Abstractions;

namespace Prometheus.Execution
{
    internal interface ICache<TKey, TValue>
    {
        TValue GetOrCreate(TKey key, Func<TValue> factory);
    }

    internal class Cache<TKey, TValue>
        : ICache<TKey, TValue>
    {
        private readonly object _sync = new object();
        private readonly LinkedList<TKey> _cachedQueries = new LinkedList<TKey>();
        private readonly int _maxItems;
        private ImmutableDictionary<TKey, TValue> _cache = ImmutableDictionary<TKey, TValue>.Empty;

        public Cache()
            : this(100)
        {
        }

        public Cache(int maxItems)
        {
            if (maxItems < 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxItems), maxItems,
                    "maxItems must be larger than 99.");
            }
            _maxItems = maxItems;
        }

        public TValue GetOrCreate(TKey key, Func<TValue> factory)
        {
            if (!_cache.TryGetValue(key, out var value))
            {
                lock (_sync)
                {
                    value = factory();

                    _cache = _cache.SetItem(key, value);
                    _cachedQueries.AddFirst(key);

                    if (_cachedQueries.Count > _maxItems)
                    {
                        _cache = _cache.Remove(_cachedQueries.Last.Value);
                        _cachedQueries.RemoveLast();
                    }
                }
            }
            return value;
        }
    }

}