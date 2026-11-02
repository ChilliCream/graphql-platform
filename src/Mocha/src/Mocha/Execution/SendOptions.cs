namespace Mocha;

/// <summary>
/// Options controlling the behavior of a send operation, such as scheduling, expiration, routing overrides, and custom headers.
/// </summary>
public readonly struct SendOptions
{
    /// <summary>
    /// Gets the scheduled delivery time, or <c>null</c> for immediate delivery.
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; init; }

    /// <summary>
    /// Gets the absolute time after which the message should be considered expired, or <c>null</c> for no expiration.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }

    /// <summary>
    /// Gets the explicit destination endpoint address, overriding the default route, or <c>null</c> to use routing conventions.
    /// </summary>
    public Uri? Endpoint { get; init; }

    /// <summary>
    /// Gets the reply endpoint address where responses should be sent, or <c>null</c> to use the default.
    /// </summary>
    public Uri? ReplyEndpoint { get; init; }

    /// <summary>
    /// Gets the fault endpoint address where fault notifications should be sent, or <c>null</c> to use the default.
    /// </summary>
    public Uri? FaultEndpoint { get; init; }

    /// <summary>
    /// Gets custom headers to include with the sent message, or <c>null</c> if none.
    /// </summary>
    public Dictionary<string, object?>? Headers { get; init; }

    /// <summary>
    /// Gets the default send options with no overrides.
    /// </summary>
    public static readonly SendOptions Default;
}
