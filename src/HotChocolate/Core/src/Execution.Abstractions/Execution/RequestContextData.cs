using System.Collections;

namespace HotChocolate.Execution;

/// <summary>
/// A thread-safe dictionary for request context data, optimized for pooling.
/// Uses a simple lock over <see cref="Dictionary{TKey, TValue}"/> rather than
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>
/// to avoid allocation on <see cref="Clear"/>.
/// </summary>
public sealed class RequestContextData : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly Dictionary<string, object?> _dictionary = [];

    /// <inheritdoc />
    public object? this[string key]
    {
        get
        {
            lock (_lock)
            {
                return _dictionary[key];
            }
        }
        set
        {
            lock (_lock)
            {
                _dictionary[key] = value;
            }
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _dictionary.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc cref="IDictionary{TKey, TValue}.Keys" />
    public ICollection<string> Keys
    {
        get
        {
            lock (_lock)
            {
                return [.. _dictionary.Keys];
            }
        }
    }

    /// <inheritdoc cref="IDictionary{TKey, TValue}.Values" />
    public ICollection<object?> Values
    {
        get
        {
            lock (_lock)
            {
                return [.. _dictionary.Values];
            }
        }
    }

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
    {
        get
        {
            lock (_lock)
            {
                return [.. _dictionary.Keys];
            }
        }
    }

    /// <inheritdoc />
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
    {
        get
        {
            lock (_lock)
            {
                return [.. _dictionary.Values];
            }
        }
    }

    /// <inheritdoc />
    public void Add(string key, object? value)
    {
        lock (_lock)
        {
            _dictionary.Add(key, value);
        }
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, object?> item)
    {
        lock (_lock)
        {
            _dictionary.Add(item.Key, item.Value);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _dictionary.Clear();
        }
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, object?> item)
    {
        lock (_lock)
        {
            return ((ICollection<KeyValuePair<string, object?>>)_dictionary).Contains(item);
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _dictionary.ContainsKey(key);
        }
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        lock (_lock)
        {
            ((ICollection<KeyValuePair<string, object?>>)_dictionary).CopyTo(array, arrayIndex);
        }
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        lock (_lock)
        {
            return _dictionary.Remove(key);
        }
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, object?> item)
    {
        lock (_lock)
        {
            return ((ICollection<KeyValuePair<string, object?>>)_dictionary).Remove(item);
        }
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value)
    {
        lock (_lock)
        {
            return _dictionary.TryGetValue(key, out value);
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        KeyValuePair<string, object?>[] snapshot;

        lock (_lock)
        {
            snapshot = [.. _dictionary];
        }

        return ((IEnumerable<KeyValuePair<string, object?>>)snapshot).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
