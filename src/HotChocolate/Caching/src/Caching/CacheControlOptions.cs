namespace HotChocolate.Caching;

public class CacheControlOptions : ICacheControlOptions
{
    public bool Enable { get; set; } = true;

    public int DefaultMaxAge { get; set; } = 0;

    public bool ApplyDefaults { get; set; } = true;

    public GetSessionIdDelegate? GetSessionId { get; set; } = null;
}