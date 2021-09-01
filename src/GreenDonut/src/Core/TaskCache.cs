using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GreenDonut
{
    /// <summary>
    /// A memorization cache for <c>DataLoader</c>.
    /// </summary>
    public sealed class TaskCache : ITaskCache
    {
        private const int _minimumSize = 10;
        private readonly object _sync = new();
        private readonly ConcurrentDictionary<TaskCacheKey, Entry> _map = new();
        private readonly int _size;
        private readonly int _order;
        private int _usage;
        private Entry? _head;

        public TaskCache(int size)
        {
            _size = size < _minimumSize ? _minimumSize : size;
            _order = Convert.ToInt32(size * 0.7);
        }

        /// <summary>
        /// Gets the maximum size of the cache.
        /// </summary>
        public int Size => _size;

        /// <summary>
        /// Gets the count of the entries inside the cache.
        /// </summary>
        public int Usage => _usage;

        public T GetOrAddTask<T>(TaskCacheKey key, Func<T> createTask) where T : Task
        {
            if (key.Type is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (createTask is null)
            {
                throw new ArgumentNullException(nameof(createTask));
            }

            var read = true;

            Entry entry = _map.GetOrAdd(key, k =>
            {
                read = false;
                return AddNewEntry(k, createTask());
            });

            if (read)
            {
                TouchEntryUnsafe(entry);
            }

            return (T)entry.Value;
        }

        /// <summary>
        /// Tries to add a single entry to the cache. It does nothing if the
        /// cache entry exists already.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A cache entry value.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="value"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A value indicating whether the add was successful.
        /// </returns>
        public bool TryAdd<T>(TaskCacheKey key, T value) where T : Task
        {
            if (key.Type is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var read = true;

            _map.GetOrAdd(key, k =>
            {
                read = false;
                return AddNewEntry(k, value);
            });

            return !read;
        }

        /// <summary>
        /// Removes a specific entry from the cache.
        /// </summary>
        /// <param name="key">A cache entry key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        public bool TryRemove(TaskCacheKey key)
        {
            if (_map.TryRemove(key, out Entry? entry))
            {
                lock (_sync)
                {
                    RemoveEntryUnsafe(entry);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the complete cache.
        /// </summary>
        public void Clear()
        {
            lock (_sync)
            {
                _map.Clear();
                _head = null;
                _usage = 0;
            }
        }

        internal TaskCacheKey[] GetKeys()
        {
            lock (_sync)
            {
                if (_head is null)
                {
                    return Array.Empty<TaskCacheKey>();
                }

                var index = 0;
                var keys = new TaskCacheKey[_usage];
                Entry current = _head!;

                do
                {
                    keys[index++] = current!.Key;
                    current = current.Next!;

                } while (current != _head);

                return keys;
            }
        }

        private Entry AddNewEntry(TaskCacheKey key, Task value)
        {
            lock (_sync)
            {
                var entry = new Entry { Key = key, Value = value };
                AppendEntryUnsafe(entry);
                ClearSpaceForNewEntryUnsafe();
                return entry;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearSpaceForNewEntryUnsafe()
        {
            while (_head is not null && _usage > _size)
            {
                Entry last = _head.Previous!;
                RemoveEntryUnsafe(last);
                _map.TryRemove(last.Key, out _);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TouchEntryUnsafe(Entry touched)
        {
            if (_order > _usage || _head == touched)
            {
                return;
            }

            lock (_sync)
            {
                if (RemoveEntryUnsafe(touched))
                {
                    AppendEntryUnsafe(touched);
                }
            }
        }

        private void AppendEntryUnsafe(Entry newEntry)
        {
            if (_head is not null)
            {
                newEntry.Next = _head;
                newEntry.Previous = _head.Previous;
                _head.Previous!.Next = newEntry;
                _head.Previous = newEntry;
                _head = newEntry;
            }
            else
            {
                newEntry.Next = newEntry;
                newEntry.Previous = newEntry;
                _head = newEntry;
            }

            _usage++;
        }

        private bool RemoveEntryUnsafe(Entry entry)
        {
            if (entry.Next == null)
            {
                return false;
            }

            if (entry.Next == entry)
            {
                _head = null;
            }
            else
            {
                entry.Next!.Previous = entry.Previous;
                entry.Previous!.Next = entry.Next;

                if (_head == entry)
                {
                    _head = entry.Next;
                }

                entry.Next = null;
                entry.Previous = null;
            }

            _usage--;
            return true;
        }

        private class Entry
        {
            public TaskCacheKey Key;
            public Task Value = default!;
            public Entry? Next;
            public Entry? Previous;
        }
    }
}
