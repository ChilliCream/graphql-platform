using System.Collections;

namespace Mocha;

/// <summary>
/// Mutable collection of key-value header pairs attached to messages flowing through the bus.
/// </summary>
/// <remarks>
/// Headers carry metadata such as correlation identifiers, saga identifiers, and transport-specific
/// attributes alongside the message body. Keys are case-sensitive and unique; setting a key that
/// already exists replaces the previous value.
/// </remarks>
public class Headers : IHeaders
{
    private readonly List<HeaderValue> _values;

    /// <summary>
    /// Creates a new empty headers collection.
    /// </summary>
    public Headers()
    {
        _values = [];
    }

    /// <summary>
    /// Creates a new headers collection populated from the given header values.
    /// </summary>
    /// <param name="values">The initial set of header values to include.</param>
    public Headers(IEnumerable<HeaderValue> values)
    {
        _values = values.ToList();
    }

    /// <summary>
    /// Creates a new empty headers collection with the specified initial capacity to reduce allocations.
    /// </summary>
    /// <param name="length">The initial capacity of the internal storage.</param>
    public Headers(int length)
    {
        _values = new List<HeaderValue>(length);
    }

    /// <inheritdoc />
    public int Count => _values.Count;

    /// <summary>
    /// Sets the header with the specified key to the given value, replacing any existing entry with the same key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="key">The header key. Must not be <see langword="null"/>.</param>
    /// <param name="value">The header value to store.</param>
    public void Set<T>(string key, T value)
    {
        for (var i = 0; i < _values.Count; i++)
        {
            if (_values[i].Key == key)
            {
                _values[i] = new HeaderValue { Key = key, Value = value };
                return;
            }
        }

        _values.Add(new HeaderValue { Key = key, Value = value });
    }

    /// <summary>
    /// Determines whether a header with the specified key exists in this collection.
    /// </summary>
    /// <param name="key">The header key to look up.</param>
    /// <returns><see langword="true"/> if a header with the key exists; otherwise, <see langword="false"/>.</returns>
    public bool ContainsKey(string key)
    {
        foreach (var headerValue in _values)
        {
            if (headerValue.Key == key)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified header key.
    /// </summary>
    /// <param name="key">The header key to look up.</param>
    /// <param name="value">When this method returns, contains the header value if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a header with the key was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(string key, out object? value)
    {
        foreach (var headerValue in _values)
        {
            if (headerValue.Key == key)
            {
                value = headerValue.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Returns the value of the header with the specified key, or <see langword="null"/> if no such header exists.
    /// </summary>
    /// <param name="key">The header key to look up.</param>
    /// <returns>The header value, or <see langword="null"/> if the key is not present.</returns>
    public object? GetValue(string key)
    {
        foreach (var headerValue in _values)
        {
            if (headerValue.Key == key)
            {
                return headerValue.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Merges the given header values into this collection, replacing any existing entries with matching keys.
    /// </summary>
    /// <param name="values">The header values to merge.</param>
    public void AddRange(IEnumerable<HeaderValue> values)
    {
        foreach (var value in values)
        {
            Set(value.Key, value.Value);
        }
    }

    /// <inheritdoc />
    public IEnumerator<HeaderValue> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    /// <summary>
    /// Removes all headers from this collection.
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }

    /// <summary>
    /// Creates a new <see cref="Headers"/> collection from a dictionary of key-value pairs.
    /// </summary>
    /// <param name="headers">The dictionary of header keys and values to populate from.</param>
    /// <returns>A new <see cref="Headers"/> instance containing the provided entries.</returns>
    public static Headers From(IDictionary<string, object?> headers)
    {
        var result = new Headers(headers.Count);
        foreach (var (key, value) in headers)
        {
            result.Set(key, value);
        }
        return result;
    }

    /// <summary>
    /// Creates a new empty <see cref="Headers"/> collection.
    /// </summary>
    /// <returns>A new empty <see cref="Headers"/> instance.</returns>
    public static Headers Empty()
    {
        return new Headers();
    }
}
