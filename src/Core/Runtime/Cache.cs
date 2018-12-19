using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Runtime
{
    public class Cache<TValue>
    {
        private const int _defaultCacheSize = 10;
        private readonly object _sync = new object();
        private readonly LinkedList<string> _ranking =
            new LinkedList<string>();
        private ImmutableDictionary<string, CacheEntry> _cache =
            ImmutableDictionary<string, CacheEntry>.Empty;
        private LinkedListNode<string> _first;

        public event EventHandler<CacheEntryEventArgs<TValue>> RemovedEntry;

        public Cache(int size)
        {
            Size = size < _defaultCacheSize ? _defaultCacheSize : size;
        }

        public int Size { get; }
        public int Usage => _cache.Count;

        public TValue GetOrCreate(string key, Func<TValue> create)
        {
            if (_cache.TryGetValue(key, out CacheEntry entry))
            {
                TouchEntry(entry.Rank);
            }
            else
            {
                entry = new CacheEntry(key, create());
                AddNewEntry(entry);
            }
            return entry.Value;
        }

        private void TouchEntry(LinkedListNode<string> rank)
        {
            if (_first != rank)
            {
                lock (_sync)
                {
                    if (_ranking.First != rank)
                    {
                        _ranking.Remove(rank);
                        _ranking.AddFirst(rank);
                        _first = rank;
                    }
                }
            }
        }

        private void AddNewEntry(CacheEntry entry)
        {
            if (!_cache.ContainsKey(entry.Key))
            {
                lock (_sync)
                {
                    if (!_cache.ContainsKey(entry.Key))
                    {
                        ClearSpaceForNewEntry();
                        _ranking.AddFirst(entry.Rank);
                        _cache = _cache.SetItem(entry.Key, entry);
                        _first = entry.Rank;
                    }
                }
            }
        }

        private void ClearSpaceForNewEntry()
        {
            if (_cache.Count >= Size)
            {
                LinkedListNode<string> rank = _ranking.Last;
                CacheEntry entry = _cache[rank.Value];
                _cache = _cache.Remove(rank.Value);
                _ranking.Remove(rank);

                RemovedEntry?.Invoke(this,
                    new CacheEntryEventArgs<TValue>(entry.Key, entry.Value));
            }
        }

        private class CacheEntry
        {
            public CacheEntry(string key, TValue value)
            {
                Key = key ?? throw new ArgumentNullException(nameof(key));
                Rank = new LinkedListNode<string>(key);
                Value = value;
            }

            public string Key { get; }
            public LinkedListNode<string> Rank { get; }
            public TValue Value { get; }
        }
    }
}
