using Microsoft.Extensions.Options;

namespace HotChocolate.Caching;

internal class CacheControlOptionsAccessor : ICacheControlOptionsAccessor
{
    public CacheControlOptionsAccessor(IOptions<CacheControlOptions> options)
    {
        CacheControl = options.Value;
    }

    public ICacheControlOptions CacheControl { get; }
}
