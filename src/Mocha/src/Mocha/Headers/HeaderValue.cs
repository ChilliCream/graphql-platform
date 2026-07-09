namespace Mocha;

/// <summary>
/// Represents a single key-value pair in a message header collection.
/// </summary>
public readonly struct HeaderValue
{
    /// <summary>
    /// Gets the header key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the header value.
    /// </summary>
    public required object? Value { get; init; }
}
