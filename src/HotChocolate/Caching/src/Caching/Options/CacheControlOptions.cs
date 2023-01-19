namespace HotChocolate.Caching;

/// <inheritdoc />
public sealed class CacheControlOptions : ICacheControlOptions
{
    /// <inheritdoc />
    public bool Enable { get; set; } = true;

    /// <inheritdoc />
    public int DefaultMaxAge { get; set; } = 0;

    /// <inheritdoc />
    public bool ApplyDefaults { get; set; } = true;
}
