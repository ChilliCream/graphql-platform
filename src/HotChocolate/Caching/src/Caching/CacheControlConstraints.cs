namespace HotChocolate.Caching;

internal sealed class CacheControlConstraints
{
    public CacheControlScope Scope { get; set; } = CacheControlScope.Public;

    internal int? MaxAge { get; set; }
}
