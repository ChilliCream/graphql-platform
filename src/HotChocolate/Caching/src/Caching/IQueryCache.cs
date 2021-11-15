using HotChocolate.Execution;
using System.Threading.Tasks;

namespace HotChocolate.Caching;

public interface IQueryCache
{
    bool ShouldReadResultFromCache(IRequestContext context);

    bool ShouldWriteResultToCache(IRequestContext context);

    Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context,
        IQueryCacheSettings settings);

    Task CacheQueryResultAsync(IRequestContext context, QueryCacheResult result,
        IQueryCacheSettings settings);
}