using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Caching;

public abstract class QueryCache
{
    public virtual bool ShouldCacheResult(IRequestContext context)
        => true;

    public abstract Task CacheQueryResultAsync(IRequestContext context,
        ICacheControlResult result, ICacheControlOptions options);
}
