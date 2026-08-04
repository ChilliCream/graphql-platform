namespace Mocha;

/// <summary>
/// Represents a mutable collection of message headers, extending <see cref="IReadOnlyHeaders"/> with write operations.
/// </summary>
public interface IHeaders : IReadOnlyHeaders
{
    /// <summary>
    /// Sets a header value, replacing any existing value with the same key.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The header key.</param>
    /// <param name="value">The header value.</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Adds multiple header values to the collection.
    /// </summary>
    /// <param name="values">The header values to add.</param>
    void AddRange(IEnumerable<HeaderValue> values);

    /// <summary>
    /// Gets the number of headers in the collection.
    /// </summary>
    int Count { get; }
}
