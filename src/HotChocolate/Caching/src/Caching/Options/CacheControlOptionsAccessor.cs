using Microsoft.Extensions.Options;

namespace HotChocolate.Caching;

/// <inheritdoc/>
internal sealed class CacheControlOptionsAccessor : ICacheControlOptionsAccessor
{
    public CacheControlOptionsAccessor(IOptions<CacheControlOptions> options)
    {
        CacheControl = options.Value;
    }

    /// <inheritdoc/>
    public ICacheControlOptions CacheControl { get; }
}
