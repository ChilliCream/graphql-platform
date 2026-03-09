namespace Mocha;

/// <summary>
/// Options controlling the behavior of a publish operation, such as scheduling, expiration, and custom headers.
/// </summary>
public readonly struct PublishOptions
{
    /// <summary>
    /// TODO this is currently not wired up
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; init; }

    /// <summary>
    /// Gets the maximum number of delivery retries before the message is dead-lettered, or <c>null</c> for the default.
    /// </summary>
    public int? MaxRetries { get; init; }

    /// <summary>
    /// Gets the absolute time after which the message should be considered expired, or <c>null</c> for no expiration.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// Gets custom headers to include with the published message, or <c>null</c> if none.
    /// </summary>
    public Dictionary<string, object?>? Headers { get; init; }

    /// <summary>
    /// Gets the default publish options with no overrides.
    /// </summary>
    public static readonly PublishOptions Default = new() { };
}
