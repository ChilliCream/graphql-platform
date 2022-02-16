namespace HotChocolate.Caching;

internal class CacheControlResult : ICacheControlResult
{
    int ICacheControlResult.MaxAge => MaxAge ?? 0;

    public CacheControlScope Scope { get; internal set; } = CacheControlScope.Public;

    internal int? MaxAge { get; set; }
}