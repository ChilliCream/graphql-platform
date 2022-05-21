using HotChocolate.Execution;
using System.Threading.Tasks;

namespace HotChocolate.Caching;

public abstract class DefaultQueryCache : IQueryCache
{
    public virtual bool ShouldReadResultFromCache(IRequestContext context)
        => true;

    public abstract Task<IQueryResult?> TryReadCachedQueryResultAsync(
        IRequestContext context, ICacheControlOptions options);

    public virtual bool ShouldCacheResult(IRequestContext context)
        => true;

    public abstract Task CacheQueryResultAsync(IRequestContext context,
        ICacheControlResult result, ICacheControlOptions options);
}
