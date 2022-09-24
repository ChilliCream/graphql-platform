using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Caching;

public abstract class QueryCache
{
    public virtual bool ShouldWriteQueryResultToCache(IRequestContext context)
        => true;

    public abstract ValueTask WriteQueryResultToCacheAsync(IRequestContext context,
        ICacheControlResult result, ICacheControlOptions options);
}
