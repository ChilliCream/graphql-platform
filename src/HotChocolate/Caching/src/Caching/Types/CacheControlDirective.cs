namespace HotChocolate.Caching;

public sealed class CacheControlDirective
{
    public CacheControlDirective(
        int? maxAge = null,
        CacheControlScope? scope = null,
        bool? inheritMaxAge = null,
        int? sharedMaxAge = null,
        string[]? vary = null)
    {
        MaxAge = maxAge;
        Scope = scope;
        InheritMaxAge = inheritMaxAge;
        SharedMaxAge = sharedMaxAge;
        Vary = vary;
    }

    public int? MaxAge { get; }

    public int? SharedMaxAge { get; }

    public CacheControlScope? Scope { get; }

    public bool? InheritMaxAge { get; }

    public string[]? Vary { get; }
}
