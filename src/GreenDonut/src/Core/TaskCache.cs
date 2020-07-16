using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GreenDonut
{
    internal class TaskCache
        : ITaskCache
        , IDisposable
    {
        private readonly ConcurrentDictionary<object, CacheEntry> _cache =
            new ConcurrentDictionary<object, CacheEntry>();
        private bool _disposed;
        private readonly LinkedList<object> _ranking = new LinkedList<object>();
        private readonly object _sync = new object();

        public TaskCache(int size)
        {
            Size = (Defaults.MinCacheSize > size)
                ? Defaults.MinCacheSize
                : size;
        }

        public int Size { get; }

        public int Usage => _cache.Count;

        public void Clear()
        {
            lock (_sync)
            {
                _ranking.Clear();
                _cache.Clear();
            }
        }

        public void Remove(object key)
        {
            lock (_sync)
            {
                if (_cache.TryRemove(key, out CacheEntry? entry))
                {
                    _ranking.Remove(entry.Rank);
                }
            }
        }

        public bool TryAdd(object key, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var added = false;

            if (!_cache.ContainsKey(key))
            {
                lock (_sync)
                {
                    if (!_cache.ContainsKey(key))
                    {
                        var entry = new CacheEntry(key, value);

                        if (_cache.TryAdd(entry.Key, entry))
                        {
                            EnsureCacheSizeDoesNotExceed();
                            _ranking.AddFirst(entry.Rank);
                            added = true;
                        }
                    }
                }
            }

            return added;
        }

        public bool TryGetValue(object key, [NotNullWhen(true)]out object? value)
        {
            object? cachedValue = null;

            lock (_sync)
            {
                if (_cache.TryGetValue(key, out CacheEntry? entry))
                {
                    TouchEntry(entry);
                    cachedValue = entry.Value;
                }
            }

            value = cachedValue;

            return value != null;
        }

        private void EnsureCacheSizeDoesNotExceed()
        {
            if (_cache.Count > Size)
            {
                object key = _ranking.Last!.Value;

                if (_cache.TryRemove(key, out CacheEntry? entry))
                {
                    _ranking.Remove(entry.Rank);
                }
            }
        }

        private void TouchEntry(CacheEntry entry)
        {
            if (_ranking.First != entry.Rank)
            {
                _ranking.Remove(entry.Rank);
                _ranking.AddFirst(entry.Rank);
            }
        }

        private class CacheEntry
        {
            public CacheEntry(object key, object value)
            {
                Key = key;
                Rank = new LinkedListNode<object>(key);
                Value = value;
            }

            public object Key { get; }

            public LinkedListNode<object> Rank { get; }

            public object Value { get; }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Clear();
                }

                _disposed = true;
            }
        }
    }
}
