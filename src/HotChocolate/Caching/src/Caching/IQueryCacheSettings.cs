namespace HotChocolate.Caching;

public interface IQueryCacheSettings
{
    bool Enable { get; }

    int DefaultMaxAge { get; }

    GetSessionIdDelegate? GetSessionId { get; }
}