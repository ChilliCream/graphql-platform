namespace HotChocolate.Caching;

/// <summary>
/// The cache control options accessor.
/// </summary>
internal interface ICacheControlOptionsAccessor
{
    /// <summary>
    /// Gets the cache control options.
    /// </summary>
    ICacheControlOptions CacheControl { get; }
}
