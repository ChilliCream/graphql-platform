namespace HotChocolate.Caching;

internal class QueryCacheOptionsAccessor : IQueryCacheOptionsAccessor
{
    public QueryCacheSettings QueryCache { get; } = new();
}