using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Caching;

/// <summary>
/// A query result cache.
/// </summary>
public abstract class QueryCache
{
    /// <summary>
    /// Returns whether the result of the given <paramref name="context"/>
    /// should be cached or not.
    /// </summary>
    /// <param name="context">The context of the request.</param>
    public virtual bool ShouldWriteQueryResultToCache(IRequestContext context)
        => true;

    /// <summary>
    /// Writes the result of the given <paramref name="context"/> to a query
    /// result store, according to the constraints given by the <paramref name="constraints"/>
    /// and the <paramref name="options"/>.
    /// </summary>
    /// <param name="context">The context of the request.</param>
    /// <param name="constraints">The constraints to which the result shall be cached./// </param>
    /// <param name="options">The cache options.</param>
    public abstract ValueTask WriteQueryResultToCacheAsync(
        IRequestContext context,
        ICacheConstraints constraints,
        ICacheControlOptions options);
}
