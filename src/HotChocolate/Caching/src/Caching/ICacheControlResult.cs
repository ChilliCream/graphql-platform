namespace HotChocolate.Caching;

public interface ICacheControlResult
{
    int MaxAge { get; }

    CacheControlScope Scope { get; }
}