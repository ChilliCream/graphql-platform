using HotChocolate.Execution;
using System.Threading.Tasks;

namespace HotChocolate.Caching;

public interface IQueryCache
{
    bool ShouldReadResultFromCache(IRequestContext context);

    Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context,
        ICacheControlOptions options);

    bool ShouldCacheResult(IRequestContext context);

    Task CacheQueryResultAsync(IRequestContext context, ICacheControlResult result,
        ICacheControlOptions options);
}
