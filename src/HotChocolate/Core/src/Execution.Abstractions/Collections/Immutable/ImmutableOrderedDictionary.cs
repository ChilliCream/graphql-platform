using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Collections.Immutable;

/// <summary>
/// Represents an immutable dictionary that preserves the order of items based on when they were added.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// An immutable dictionary is a dictionary that never changes. Any operations that would normally modify a
/// <see cref="Dictionary{TKey, TValue}"/> instead return a new immutable dictionary that represents the desired
/// modification while leaving the original dictionary unchanged.
/// </para>
/// <para>
/// This implementation preserves the order of items based on when they were added, unlike the standard
/// <see cref="ImmutableDictionary{TKey, TValue}"/> which does not guarantee any particular order.
/// </para>
/// </remarks>
public sealed class ImmutableOrderedDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly ImmutableList<TKey> _keys;
    private readonly ImmutableDictionary<TKey, TValue> _map;

    private ImmutableOrderedDictionary(ImmutableList<TKey> keys, ImmutableDictionary<TKey, TValue> map)
    {
        _keys = keys;
        _map = map;
    }

    /// <summary>
    /// Gets an empty immutable ordered dictionary.
    /// </summary>
    /// <value>An empty immutable ordered dictionary.</value>
    public static ImmutableOrderedDictionary<TKey, TValue> Empty { get; } =
        new([], ImmutableDictionary<TKey, TValue>.Empty);

    /// <summary>
    /// Gets the number of key/value pairs in the immutable ordered dictionary.
    /// </summary>
    /// <value>The number of key/value pairs.</value>
    public int Count => _map.Count;

    /// <summary>
    /// Gets the keys in the immutable ordered dictionary in their original insertion order.
    /// </summary>
    /// <value>The keys in their original insertion order.</value>
    public ImmutableList<TKey> Keys => _keys;

    /// <summary>
    /// Gets the values in the immutable ordered dictionary in their corresponding key's insertion order.
    /// </summary>
    /// <value>The values in their corresponding key's insertion order.</value>
    public IEnumerable<TValue> Values
    {
        get
        {
            foreach (var key in _keys)
            {
                yield return _map[key];
            }
        }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary.</exception>
    public TValue this[TKey key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);
            return _map[key];
        }
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
    /// Determines whether the immutable ordered dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>true if the dictionary contains the specified key; otherwise, false.</returns>
    public bool ContainsKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _map.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to get the key associated with the specified key.
    /// </summary>
    /// <param name="equalKey">The key to locate.</param>
    /// <param name="actualKey">When this method returns, contains the key associated with the specified key,
    /// if the key is found; otherwise, contains the default value for the type of the key parameter.</param>
    public bool TryGetKey(TKey equalKey, out TKey actualKey)
    {
        ArgumentNullException.ThrowIfNull(equalKey);

        return _map.TryGetKey(equalKey, out actualKey);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, contains the default value for the type of the value parameter.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _map.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the immutable ordered dictionary.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the dictionary in insertion order.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var key in _keys)
        {
            yield return new KeyValuePair<TKey, TValue>(key, _map[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified key-value pair added.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>A new immutable ordered dictionary that contains the additional key-value pair.</returns>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public ImmutableOrderedDictionary<TKey, TValue> Add(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_map.ContainsKey(key))
        {
            throw new ArgumentException(
                "An item with the same key has already been added.",
                nameof(key));
        }

        return new ImmutableOrderedDictionary<TKey, TValue>(
            _keys.Add(key),
            _map.Add(key, value));
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(
        TKey key,
        TValue value)
        => Add(key, value);

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified key-value pair inserted at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the key-value pair.</param>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value to insert.</param>
    public ImmutableOrderedDictionary<TKey, TValue> Insert(int index, TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (index < 0 || index > _keys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new ImmutableOrderedDictionary<TKey, TValue>(
            _keys.Insert(index, key),
            _map.Add(key, value));
    }

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified key-value pair added or updated.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A new immutable ordered dictionary that contains the specified key-value pair.</returns>
    public ImmutableOrderedDictionary<TKey, TValue> SetItem(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_map.ContainsKey(key))
        {
            // Key exists, update the value
            return new ImmutableOrderedDictionary<TKey, TValue>(
                _keys,
                _map.SetItem(key, value));
        }
        // Key doesn't exist, add it to both collections
        return new ImmutableOrderedDictionary<TKey, TValue>(
            _keys.Add(key),
            _map.Add(key, value));
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(
        TKey key,
        TValue value)
        => SetItem(key, value);

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified key removed.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>A new immutable ordered dictionary with the specified key removed.</returns>
    public ImmutableOrderedDictionary<TKey, TValue> Remove(TKey key)
    {
        if (!_map.ContainsKey(key))
        {
            return this;
        }

        return new ImmutableOrderedDictionary<TKey, TValue>(
            _keys.Remove(key),
            _map.Remove(key));
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);

    /// <summary>
    /// Retrieves an empty immutable ordered dictionary.
    /// </summary>
    /// <returns>An empty immutable ordered dictionary.</returns>
    public ImmutableOrderedDictionary<TKey, TValue> Clear() => Empty;

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear() => Clear();

    /// <summary>
    /// Creates a new builder that can be used to efficiently create new immutable ordered dictionaries.
    /// </summary>
    /// <returns>A builder for the immutable ordered dictionary.</returns>
    public Builder ToBuilder() => new(_keys, _map);

    /// <summary>
    /// Determines whether the immutable ordered dictionary contains the specified key-value pair.
    /// </summary>
    /// <param name="item">The key-value pair to locate.</param>
    /// <returns>true if the dictionary contains the specified key-value pair; otherwise, false.</returns>
    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _map.Contains(item);

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified keys removed.
    /// </summary>
    /// <param name="keys">The keys to remove.</param>
    /// <returns>A new immutable ordered dictionary with the specified keys removed.</returns>
    public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var result = this;

        foreach (var key in keys)
        {
            result = result.Remove(key);
        }

        return result;
    }

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified key-value pairs added.
    /// Throws if any key already exists.
    /// </summary>
    /// <param name="pairs">The key-value pairs to add.</param>
    /// <returns>A new immutable ordered dictionary with the additional pairs.</returns>
    /// <exception cref="ArgumentException">If any key already exists.</exception>
    public ImmutableOrderedDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        var keys = _keys;
        var map = _map;
        foreach (var pair in pairs)
        {
            if (map.ContainsKey(pair.Key))
            {
                throw new ArgumentException(
                    $"An item with the same key has already been added: {pair.Key}",
                    nameof(pairs));
            }
            keys = keys.Add(pair.Key);
            map = map.Add(pair.Key, pair.Value);
        }
        return new ImmutableOrderedDictionary<TKey, TValue>(keys, map);
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(
        IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        => AddRange(pairs);

    /// <summary>
    /// Creates a new immutable ordered dictionary with the specified key-value pairs set (add or update).
    /// New keys are appended to the end.
    /// </summary>
    /// <param name="items">The key-value pairs to set.</param>
    /// <returns>A new immutable ordered dictionary with the specified items set.</returns>
    public ImmutableOrderedDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var keys = _keys;
        var map = _map;

        foreach (var pair in items)
        {
            if (map.ContainsKey(pair.Key))
            {
                map = map.SetItem(pair.Key, pair.Value);
            }
            else
            {
                keys = keys.Add(pair.Key);
                map = map.Add(pair.Key, pair.Value);
            }
        }
        return new ImmutableOrderedDictionary<TKey, TValue>(keys, map);
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(
        IEnumerable<KeyValuePair<TKey, TValue>> items)
        => SetItems(items);

    /// <summary>
    /// Represents a builder for creating immutable ordered dictionaries.
    /// </summary>
    /// <remarks>
    /// Use this class to efficiently build immutable ordered dictionaries by applying multiple mutations
    /// before creating a new immutable instance.
    /// </remarks>
    public sealed class Builder
    {
        private readonly OrderedDictionary<TKey, TValue> _dictionary;

        internal Builder()
        {
            _dictionary = [];
        }

        internal Builder(ImmutableList<TKey> keys, ImmutableDictionary<TKey, TValue> map)
        {
            _dictionary = [];

            foreach (var key in keys)
            {
                _dictionary.Add(key, map[key]);
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs in the builder.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets the keys in the builder in their original insertion order.
        /// </summary>
        public IEnumerable<TKey> Keys => _dictionary.Keys;

        /// <summary>
        /// Gets the values in the builder in their corresponding key's insertion order.
        /// </summary>
        public IEnumerable<TValue> Values => _dictionary.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key.</returns>
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        /// <summary>
        /// Adds the specified key and value to the builder.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        public void Add(TKey key, TValue value)
            => _dictionary.Add(key, value);

        /// <summary>
        /// Adds the specified key-value pairs to the builder.
        /// </summary>
        /// <param name="pairs">The key-value pairs to add.</param>
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            ArgumentNullException.ThrowIfNull(pairs);

            foreach (var pair in pairs)
            {
                _dictionary.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Inserts the specified key and value at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the key-value pair.</param>
        /// <param name="key">The key to insert.</param>
        /// <param name="value">The value to insert.</param>
        public void Insert(int index, TKey key, TValue value)
            => _dictionary.Insert(index, key, value);

        /// <summary>
        /// Removes the specified key and its associated value from the builder.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>true if the key was found and removed; otherwise, false.</returns>
        public bool Remove(TKey key) => _dictionary.Remove(key);

        /// <summary>
        /// Removes the key-value pair at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the key-value pair to remove.</param>
        public void RemoveAt(int index) => _dictionary.RemoveAt(index);

        /// <summary>
        /// Removes all keys and values from the builder.
        /// </summary>
        public void Clear() => _dictionary.Clear();

        /// <summary>
        /// Determines whether the builder contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>true if the builder contains the specified key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
            => _dictionary.ContainsKey(key);

        /// <summary>
        /// Creates an immutable ordered dictionary from the current contents of the builder.
        /// </summary>
        /// <returns>A new immutable ordered dictionary containing the current contents of the builder.</returns>
        public ImmutableOrderedDictionary<TKey, TValue> ToImmutable()
            => new([.. _dictionary.Keys], ImmutableDictionary.CreateRange(_dictionary));
    }
}

/// <summary>
/// Provides static methods for creating immutable ordered dictionaries.
/// </summary>
public static class ImmutableOrderedDictionary
{
    /// <summary>
    /// Creates a new builder for an immutable ordered dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <returns>A new builder for an immutable ordered dictionary.</returns>
    public static ImmutableOrderedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>()
        where TKey : IEquatable<TKey>
        => new();
}
