using System.Collections.Immutable;

namespace HotChocolate.Caching;

internal sealed class ImmutableCacheConstraints(
    int? maxAge,
    int? sharedMaxAge,
    CacheControlScope scope,
    ImmutableArray<string> vary)
    : ICacheConstraints
{
    public int? MaxAge { get; } = maxAge;

    public int? SharedMaxAge { get; } = sharedMaxAge;

    public CacheControlScope Scope { get; } = scope;

    public ImmutableArray<string> Vary { get; } = vary;
}
