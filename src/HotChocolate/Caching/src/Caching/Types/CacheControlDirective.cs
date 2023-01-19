namespace HotChocolate.Caching;

public sealed class CacheControlDirective
{
    public CacheControlDirective(
        int? maxAge = null,
        CacheControlScope? scope = null,
        bool? inheritMaxAge = null)
    {
        MaxAge = maxAge;
        Scope = scope;
        InheritMaxAge = inheritMaxAge;
    }

    public int? MaxAge { get; }

    public CacheControlScope? Scope { get; }

    public bool? InheritMaxAge { get; }
}
