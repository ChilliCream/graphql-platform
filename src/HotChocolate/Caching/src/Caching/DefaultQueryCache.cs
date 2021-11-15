using HotChocolate.Execution;
using System.Threading.Tasks;

namespace HotChocolate.Caching;

public abstract class DefaultQueryCache : IQueryCache
{
    public abstract Task CacheQueryResultAsync(IRequestContext context,
        QueryCacheResult result, IQueryCacheSettings settings);

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

        // todo: check if it's a query - we do not want to cache mutations / subscriptions

        if (result.Errors is { Count: > 0 })
        {
            // result has unexpected errors, we do not want to cache it

            return false;
        }

        return true;
    }
}