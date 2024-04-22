namespace HotChocolate.Caching;

/// <inheritdoc />
public sealed class CacheControlOptions : ICacheControlOptions
{
    /// <inheritdoc />
    public bool Enable { get; set; } = true;

    /// <inheritdoc />
    public int DefaultMaxAge { get; set; } = CacheControlDefaults.MaxAge;

    /// <inheritdoc />
    public CacheControlScope DefaultScope { get; set; } = CacheControlDefaults.Scope;

    /// <inheritdoc />
    public bool ApplyDefaults { get; set; } = true;
}
