namespace HotChocolate.Caching;

public class QueryCacheSettings : IQueryCacheSettings
{
    public bool Enable { get; set; } = true;

    public int DefaultMaxAge { get; set; } = 0;

    public GetSessionIdDelegate? GetSessionId { get; set; }
}