namespace HotChocolate.Caching;

public class QueryCacheResult
{
    public int MaxAge { get; set; }

    public CacheControlScope Scope { get; set; }
}