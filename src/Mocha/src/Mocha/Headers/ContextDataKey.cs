namespace Mocha;

/// <summary>
/// Represents a strongly-typed key for storing and retrieving values from message headers or context data collections.
/// </summary>
/// <typeparam name="T">The type of the value associated with this key.</typeparam>
/// <param name="key">The string key used for storage and lookup.</param>
internal sealed class ContextDataKey<T>(string key)
{
    /// <summary>
    /// The string key used for storage and lookup in header dictionaries.
    /// </summary>
    public readonly string Key = key;
}
