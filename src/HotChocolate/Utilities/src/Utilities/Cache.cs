using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities
{
    public class Cache<TValue>
    {
        private const int _minimumSize = 10;
        private readonly object _sync = new();
        private readonly LinkedList<string> _ranking = new();
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private LinkedListNode<string>? _first;

        public Cache(int size)
        {
            Size = size < _minimumSize ? _minimumSize : size;
        }

        public int Size { get; }

        public int Usage => _cache.Count;

        public bool TryGet(string key, [MaybeNull] out TValue value)
        {
            if (_cache.TryGetValue(key, out CacheEntry? entry))
            {
                TouchEntry(entry.Rank);
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        public TValue GetOrCreate(string key, Func<TValue> create)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (create is null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            if (_cache.TryGetValue(key, out CacheEntry? entry))
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

        public void Clear()
        {
            lock (_sync)
            {
                _cache.Clear();
                _ranking.Clear();
                _first = null;
            }
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
            if (_cache.TryAdd(entry.Key, entry))
            {
                lock (_sync)
                {
                    ClearSpaceForNewEntry();
                    _ranking.AddFirst(entry.Rank);
                    _first = entry.Rank;
                }
            }
        }

        private void ClearSpaceForNewEntry()
        {
            if (_cache.Count > Size)
            {
                LinkedListNode<string>? rank = _ranking.Last;
                if (rank is { } && _cache.TryRemove(rank.Value, out CacheEntry? entry))
                {
                    _ranking.Remove(rank);
                }
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
