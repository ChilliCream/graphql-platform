namespace HotChocolate.Caching;

internal class CacheControlOptionsAccessor : ICacheControlOptionsAccessor
{
    public CacheControlOptions CacheControl { get; } = new();
}