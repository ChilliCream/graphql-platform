namespace HotChocolate.Caching;

public interface ICacheControlOptions
{
    bool Enable { get; }

    int DefaultMaxAge { get; }

    bool ApplyDefaults { get; }

    GetSessionIdDelegate? GetSessionId { get; }
}