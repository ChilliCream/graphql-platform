namespace HotChocolate.Utilities;

/// <summary>
/// Represents cache entry event args.
/// </summary>
public sealed class CacheEntryEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CacheEntryEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="key">The cache entry key.</param>
    /// <param name="value">The cache entry value.</param>
    internal CacheEntryEventArgs(string key, TValue value)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value;
    }

    /// <summary>
    /// Gets the cache entry key.
    /// </summary>
    /// <value>The key.</value>
    public string Key { get; }

    /// <summary>
    /// Gets the cache entry value.
    /// </summary>
    /// <value>The value.</value>
    public TValue Value { get; }
}
