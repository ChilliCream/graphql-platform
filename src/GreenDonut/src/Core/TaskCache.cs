using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut;

/// <summary>
/// A memorization cache for <c>DataLoader</c>.
/// </summary>
public sealed class TaskCache : ITaskCache
{
    private const int _minimumSize = 10;
    private readonly object _sync = new();
    private readonly ReaderWriterLockSlim _observableSync = new();
    private readonly ConcurrentDictionary<TaskCacheKey, Entry> _map = new();
    private readonly List<IObserver<TaskCacheResult>> _observers = new();
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
    public Task<T> GetOrAddTask<T>(TaskCacheKey key, Func<Task<T>> createTask)
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

        var entry = _map.GetOrAdd(key,
            k =>
            {
                read = false;
                return AddNewEntry(k, createTask());
            });

        if (read)
        {
            TouchEntryUnsafe(entry);
        }

        return (Task<T>)entry.Value;
    }

    /// <inheritdoc />
    public bool TryAdd<T>(TaskCacheKey key, Task<T> value)
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

        _map.GetOrAdd(key,
            k =>
            {
                read = false;
                return AddNewEntry(k, value);
            });

        return !read;
    }

    /// <inheritdoc />
    public bool TryAdd<T>(TaskCacheKey key, Func<Task<T>> createTask)
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

        _map.GetOrAdd(key,
            k =>
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
            _observers.Clear();
            _head = null;
            _usage = 0;
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<TaskCacheResult> observer)
    {
        // We enter the write lock so we do not modify the list concurrently
        _observableSync.EnterWriteLock();
        try
        {
            _observers.Add(observer);
        }
        finally
        {
            _observableSync.ExitWriteLock();
        }

        foreach (KeyValuePair<TaskCacheKey, Entry> entry in _map)
        {
            if (entry.Value.Result is not null)
            {
                observer.OnNext(entry.Value.Result);
            }
        }

        return new Unsubscriber(observer, Unsubscribe);
    }

    private Entry AddNewEntry<T>(TaskCacheKey key, Task<T> value)
    {
        Entry entry;
        lock (_sync)
        {
            entry = new Entry { Key = key, Value = value };
            AppendEntryUnsafe(entry);
            ClearSpaceForNewEntryUnsafe();
        }

        value.ContinueWith(x =>
        {
            if (x.Result is null)
            {
                return;
            }

            TaskCacheResult result = new(key, x.Result);
            entry.Result = result;
            Publish(result);
        });

        return entry;
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


    private void Publish(TaskCacheResult value)
    {
        // we enter the read lock so that multiple threads can publish their data in
        // parallel
        _observableSync.EnterReadLock();
        try
        {
            for (var i = 0; i < _observers.Count; i++)
            {
                _observers[i].OnNext(value);
            }
        }
        finally
        {
            _observableSync.ExitReadLock();
        }
    }

    private void Unsubscribe(IObserver<TaskCacheResult> observer)
    {
        _observableSync.EnterWriteLock();
        try
        {
            _observers.Remove(observer);
        }
        finally
        {
            _observableSync.ExitWriteLock();
        }
    }

    private class Entry
    {
        public TaskCacheKey Key;
        public Task Value = default!;
        public Entry? Next;
        public Entry? Previous;
        public TaskCacheResult? Result { get; set; }
    }

    private class Unsubscriber : IDisposable
    {
        private IObserver<TaskCacheResult> _observer;
        private readonly Action<IObserver<TaskCacheResult>> _unsubscribe;

        public Unsubscriber(
            IObserver<TaskCacheResult> observer,
            Action<IObserver<TaskCacheResult>> unsubscribe)
        {
            _observer = observer;
            _unsubscribe = unsubscribe;
        }

        public void Dispose() => _unsubscribe(_observer);
    }
}
