namespace HotChocolate.Caching;

/// <summary>
/// The query cache options accessor.
/// </summary>
public interface IQueryCacheOptionsAccessor
{
    /// <summary>
    /// Gets the query cache settings.
    /// </summary>
    QueryCacheSettings QueryCache { get; }
}