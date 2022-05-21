namespace HotChocolate.Caching;

/// <summary>
/// The cache control options accessor.
/// </summary>
public interface ICacheControlOptionsAccessor
{
    /// <summary>
    /// Gets the cache control options.
    /// </summary>
    ICacheControlOptions CacheControl { get; }
}
