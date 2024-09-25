namespace HotChocolate.Caching;

internal sealed class ImmutableCacheConstraints : ICacheConstraints
{
    public ImmutableCacheConstraints(
        int? maxAge,
        int? sharedMaxAge,
        CacheControlScope scope,
        IReadOnlyCollection<string> vary)
    {
        MaxAge = maxAge;
        SharedMaxAge = sharedMaxAge;
        Scope = scope;
        Vary = vary;
    }

    public int? MaxAge { get; }

    public int? SharedMaxAge { get; }

    public CacheControlScope Scope { get; }

    public IReadOnlyCollection<string> Vary { get; }
}
