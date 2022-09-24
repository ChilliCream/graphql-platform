namespace HotChocolate.Caching;

internal class CacheControlConstraints : ICacheConstraints
{
    /// <inheritdoc />
    int ICacheConstraints.MaxAge => MaxAge ?? 0;

    /// <inheritdoc />
    public CacheControlScope Scope { get; internal set; } = CacheControlScope.Public;

    internal int? MaxAge { get; set; }
}
