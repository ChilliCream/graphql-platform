namespace Mocha;

/// <summary>
/// Represents a read-only collection of message headers that supports lookup by key and enumeration.
/// </summary>
public interface IReadOnlyHeaders : IEnumerable<HeaderValue>
{
    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The header key to look up.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    bool TryGetValue(string key, out object? value);

    /// <summary>
    /// Gets the value associated with the specified key, or <c>null</c> if the key is not found.
    /// </summary>
    /// <param name="key">The header key to look up.</param>
    /// <returns>The header value, or <c>null</c> if not found.</returns>
    object? GetValue(string key);

    /// <summary>
    /// Determines whether the collection contains a header with the specified key.
    /// </summary>
    /// <param name="key">The header key to check.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    bool ContainsKey(string key);
}
