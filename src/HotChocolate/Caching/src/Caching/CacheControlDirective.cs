using System;

namespace HotChocolate.Caching;

public class CacheControlDirective
{
    public CacheControlDirective(int? maxAge = null,
        CacheControlScope? scope = null,
        bool? inheritMaxAge = null)
    {
        if (maxAge.HasValue && maxAge.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAge), maxAge, "TODO");
        }

        MaxAge = maxAge;
        Scope = scope;
        InheritMaxAge = inheritMaxAge;
    }

    public int? MaxAge { get; }

    public CacheControlScope? Scope { get; }

    public bool? InheritMaxAge { get; set; }
}