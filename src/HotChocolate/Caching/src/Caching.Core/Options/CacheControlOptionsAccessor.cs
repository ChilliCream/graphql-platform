using Microsoft.Extensions.Options;

namespace HotChocolate.Caching;

/// <inheritdoc/>
internal sealed class CacheControlOptionsAccessor(
    IOptions<CacheControlOptions> options)
    : ICacheControlOptionsAccessor
{
    /// <inheritdoc/>
    public ICacheControlOptions CacheControl { get; } = options.Value;
}
