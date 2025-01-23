#if NET8_0
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic;

public class OrderedDictionary<TKey, TValue>
    : IDictionary<TKey, TValue>
    , IReadOnlyDictionary<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly List<TKey> _keys = [];
    private readonly Dictionary<TKey, TValue> _map = new();

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _map.TryGetValue(key, out value);

    public TValue this[TKey key]
    {
        get
        {
            return _map[key];
        }
        set
        {
            if (_map.ContainsKey(key))
            {
                _map[key] = value;
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public ICollection<TKey> Keys => _map.Keys;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        Keys;

    public ICollection<TValue> Values => _map.Values;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        Values;

    public int Count => _keys.Count;

    public bool IsReadOnly => false;

    public void Insert(int index, TKey key, TValue value)
    {
        if (index < 0 || index > _keys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _map.Add(key, value);
        _keys.Insert(index, key);
    }

    public void Add(TKey key, TValue value)
    {
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _map.Add(item.Key, item.Value);
        _keys.Add(item.Key);
    }

    public void Clear()
    {
        _map.Clear();
        _keys.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _keys.Contains(item.Key);

    public bool ContainsKey(TKey key)
        => _map.ContainsKey(key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < _keys.Count)
        {
            throw new ArgumentException(
                "The number of elements in the source collection is greater than the available space from the index to the end of the destination array.");
        }

        foreach (var item in _keys)
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(item, _map[item]);
        }
    }

    public bool Remove(TKey key)
    {
        if (_map.ContainsKey(key))
        {
            _map.Remove(key);
            _keys.RemoveAt(IndexOfKey(key));
            return true;
        }
        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = IndexOfKey(item.Key);
        if(index != -1)
        {
            var value = _map[item.Key];
            if(value == null || value.Equals(item.Value))
            {
                _keys.RemoveAt(index);
                _map.Remove(item.Key);
                return true;
            }
        }

        return false;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _keys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var key = _keys[index];
        _map.Remove(key);
        _keys.RemoveAt(index);
        return true;
    }

    public int IndexOf(TKey key)
        => IndexOfKey(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOfKey(TKey key)
    {
        var span = CollectionsMarshal.AsSpan(_keys);
        return span.IndexOf(key);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var key in _keys)
        {
            yield return new KeyValuePair<TKey, TValue>(key, _map[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => _keys.GetEnumerator();
}
#endif
