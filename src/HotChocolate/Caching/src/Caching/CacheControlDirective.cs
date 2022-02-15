namespace HotChocolate.Caching;

public class CacheControlDirective
{
    public CacheControlDirective()
    {

    }

    public CacheControlDirective(int? maxAge = null,
        CacheControlScope? scope = null,
        bool? inheritMaxAge = null)
    {
        MaxAge = maxAge;
        Scope = scope;
        InheritMaxAge = inheritMaxAge;
    }

    public int? MaxAge { get; set; }

    public CacheControlScope? Scope { get; set; }

    public bool? InheritMaxAge { get; set; }
}