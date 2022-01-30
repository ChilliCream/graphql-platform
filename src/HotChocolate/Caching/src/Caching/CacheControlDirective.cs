using System;

namespace HotChocolate.Caching;

public class CacheControlDirective
{
    private int? maxAge;

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

    public int? MaxAge
    {
        get => maxAge; set
        {
            if (value.HasValue && value.Value < 0)
            {
                // todo: better exception
                throw new Exception($"{nameof(MaxAge)} can not be set to a value less than 0.");
            }

            maxAge = value;
        }
    }

    public CacheControlScope? Scope { get; set; }

    public bool? InheritMaxAge { get; set; }
}