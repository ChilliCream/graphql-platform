using HotChocolate.Execution;
using HotChocolate.Language;
using System.Threading.Tasks;

namespace HotChocolate.Caching;

public abstract class DefaultQueryCache : IQueryCache
{
    public abstract Task CacheQueryResultAsync(IRequestContext context,
        CacheControlResult result, IQueryCacheSettings settings);

    public abstract Task<IQueryResult?> TryReadCachedQueryResultAsync(
        IRequestContext context, IQueryCacheSettings settings);

    public virtual bool ShouldReadResultFromCache(IRequestContext context)
    {
        return true;
    }

    public virtual bool ShouldWriteResultToCache(IRequestContext context)
    {
        if (context.Result is not IReadOnlyQueryResult result)
        {
            return false;
        }

        // operations other than query should not be cached.
        if (context.Operation is null
            || context.Operation.Definition.Operation != OperationType.Query)
        {
            return false;
        }

        if (result.Errors is { Count: > 0 })
        {
            // result has unexpected errors, we do not want to cache it

            return false;
        }

        return true;
    }
}