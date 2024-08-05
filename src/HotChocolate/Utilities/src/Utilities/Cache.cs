using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace HotChocolate.Utilities;

#pragma warning disable CA1724
public sealed class Cache<TValue>(int size)
{
    private const int _minimumSize = 10;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<string, Entry> _map = new(StringComparer.Ordinal);
    private readonly int _capacity = size < _minimumSize ? _minimumSize : size;
    private readonly int _order = Convert.ToInt32(size * 0.7);
    private int _usage;
    private Entry? _head;

    /// <summary>
    /// Gets the maximum allowed item count that can be stored in this cache.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the current item count that is currently stored in this cache.
    /// </summary>
    public int Usage => _usage;

    public bool TryGet(string key, out TValue? value)
    {
        if (_map.TryGetValue(key, out var entry))
        {
            TouchEntryUnsafe(entry);
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

        var read = true;

        var entry = _map.GetOrAdd(key, k =>
        {
            read = false;
            return AddNewEntry(k, create());
        });

        if (read)
        {
            TouchEntryUnsafe(entry);
        }

        return entry.Value;
    }

    public void Clear()
    {
        lock (_sync)
        {
            _map.Clear();
            _head = null;
            _usage = 0;
        }
    }

    internal string[] GetKeys()
    {
        lock (_sync)
        {
            if (_head is null)
            {
                return [];
            }

            var index = 0;
            var keys = new string[_usage];
            var current = _head!;

            do
            {
                keys[index++] = current.Key;
                current = current.Next!;
            } while (current != _head);

            return keys;
        }
    }

    private Entry AddNewEntry(string key, TValue value)
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
        while (_head is not null && _usage > _capacity)
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
        public string Key = default!;
        public TValue Value = default!;
        public Entry? Next;
        public Entry? Previous;
    }
}
#pragma warning restore CA1724
