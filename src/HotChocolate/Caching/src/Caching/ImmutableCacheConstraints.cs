namespace HotChocolate.Caching;

internal sealed class ImmutableCacheConstraints : ICacheConstraints
{
    public ImmutableCacheConstraints(int maxAge, CacheControlScope scope)
    {
        MaxAge = maxAge;
        Scope = scope;
    }

    public int MaxAge { get; }

    public CacheControlScope Scope { get; }
}
