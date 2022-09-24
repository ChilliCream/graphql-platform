using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Caching;

public abstract class QueryCache
{
    public virtual bool ShouldWriteQueryToCache(IRequestContext context)
        => true;

    public abstract ValueTask WriteQueryToCacheAsync(IRequestContext context,
        ICacheControlResult result, ICacheControlOptions options);
}
