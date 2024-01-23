using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GreenDonut;

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

    /// <summary>
    /// Creates a new instance of <see cref="TaskCache"/>.
    /// </summary>
    /// <param name="size">
    /// The size of the cache. The minimum cache size is 10.
    /// </param>
    public TaskCache(int size)
    {
        _size = size < _minimumSize ? _minimumSize : size;
        _order = Convert.ToInt32(size * 0.7);
    }

    /// <inheritdoc />
    public int Size => _size;

    /// <inheritdoc />
    public int Usage => _usage;

    /// <inheritdoc />
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

        var entry = _map.GetOrAdd(key, k =>
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool TryAdd<T>(TaskCacheKey key, Func<T> createTask) where T : Task
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

        _map.GetOrAdd(key, k =>
        {
            read = false;
            return AddNewEntry(k, createTask());
        });

        return !read;
    }

    /// <inheritdoc />
    public bool TryRemove(TaskCacheKey key)
    {
        if (_map.TryRemove(key, out var entry))
        {
            lock (_sync)
            {
                RemoveEntryUnsafe(entry);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_sync)
        {
            _map.Clear();
            _head = null;
            _usage = 0;
        }
    }

    private Entry AddNewEntry(TaskCacheKey key, Task value)
    {
        lock (_sync)
        {
            var entry = new Entry { Key = key, Value = value, };
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
            var last = _head.Previous!;
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

    private sealed class Entry
    {
        public TaskCacheKey Key;
        public Task Value = default!;
        public Entry? Next;
        public Entry? Previous;
    }
}