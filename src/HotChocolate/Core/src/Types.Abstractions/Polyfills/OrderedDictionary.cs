#if NET8_0
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic;

/// <summary>
/// Represents a generic collection of key/value pairs that are ordered by the sequence of insertion.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class OrderedDictionary<TKey, TValue>
    : IDictionary<TKey, TValue>
    , IReadOnlyDictionary<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly List<TKey> _keys = [];
    private readonly Dictionary<TKey, TValue> _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that is empty.
    /// </summary>
    public OrderedDictionary()
    {
        _map = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new dictionary.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> is null.</exception>
    public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        _map = [];

        foreach (var (key, value) in collection)
        {
            _map.Add(key, value);
            _keys.Add(key);
        }
    }

    public OrderedDictionary(IEqualityComparer<TKey>? comparer)
    {
        _map = new Dictionary<TKey, TValue>(comparer);
    }

    /// <summary>
    /// Gets the key/value pair at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The key/value pair at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
    public KeyValuePair<TKey, TValue> GetAt(int index)
    {
        if (index < 0 || index >= _keys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var key = _keys[index];
        return new KeyValuePair<TKey, TValue>(key, _map[key]);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _map.TryGetValue(key, out value);

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">The property is retrieved and key is not found.</exception>
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

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    public ICollection<TKey> Keys => _keys;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        Keys;

    /// <summary>
    /// Gets a collection containing the values in the dictionary.
    /// </summary>
    public ICollection<TValue> Values => _map.Values;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        Values;

    /// <summary>
    /// Gets the number of key/value pairs contained in the dictionary.
    /// </summary>
    public int Count => _keys.Count;

    /// <summary>
    /// Gets a value indicating whether the dictionary is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Inserts a key-value pair into the dictionary at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the key-value pair should be inserted.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown if the key already exists in the dictionary.</exception>
    public void Insert(int index, TKey key, TValue value)
    {
        if (index < 0 || index > _keys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _map.Add(key, value);
        _keys.Insert(index, key);
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists in the dictionary.</exception>
    public void Add(TKey key, TValue value)
    {
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    /// <summary>
    /// Adds the specified key-value pair to the dictionary.
    /// </summary>
    /// <param name="item">The key-value pair to add.</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists in the dictionary.</exception>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _map.Add(item.Key, item.Value);
        _keys.Add(item.Key);
    }

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    public void Clear()
    {
        _map.Clear();
        _keys.Clear();
    }

    /// <summary>
    /// Determines whether the dictionary contains a specific key-value pair.
    /// </summary>
    /// <param name="item">The key-value pair to locate in the dictionary.</param>
    /// <returns>true if the dictionary contains the specified key-value pair; otherwise, false.</returns>
    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _keys.Contains(item.Key);

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(TKey key)
        => _map.ContainsKey(key);

    /// <summary>
    /// Copies the elements of the dictionary to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the dictionary.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="array"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="arrayIndex"/> is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown if the number of elements in the source collection is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

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

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
    public bool Remove(TKey key)
    {
        if (_map.Remove(key))
        {
            _keys.RemoveAt(IndexOfKey(key));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the specified key-value pair from the dictionary.
    /// </summary>
    /// <param name="item">The key-value pair to remove.</param>
    /// <returns>true if the key-value pair is successfully found and removed; otherwise, false.</returns>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = IndexOfKey(item.Key);
        if (index != -1)
        {
            var value = _map[item.Key];
            if (value == null || value.Equals(item.Value))
            {
                _keys.RemoveAt(index);
                _map.Remove(item.Key);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes the element at the specified index of the dictionary.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
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

    /// <summary>
    /// Returns the zero-based index of the specified key in the dictionary.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns>The zero-based index of the key if found; otherwise, -1.</returns>
    public int IndexOf(TKey key)
        => IndexOfKey(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOfKey(TKey key)
    {
        var span = CollectionsMarshal.AsSpan(_keys);
        return span.IndexOf(key);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary in insertion order.
    /// </summary>
    /// <returns>An enumerator for the dictionary.</returns>
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
