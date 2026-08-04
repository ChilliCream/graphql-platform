namespace Mocha.Events;

/// <summary>
/// Defines well-known error code constants used in <see cref="NotAcknowledgedEvent"/> and fault reporting.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Indicates a general unhandled exception during message processing.
    /// </summary>
    public const string Exception = "Exception";

    /// <summary>
    /// Indicates that a local timeout occurred before a response was received.
    /// </summary>
    public const string LocalTimeout = "LocalTimeout";

    /// <summary>
    /// Indicates that the message expired before delivery or processing.
    /// </summary>
    public const string Expired = "MessageExpired";

    /// <summary>
    /// Indicates that the maximum number of delivery retries was reached.
    /// </summary>
    public const string MaxRetryReached = "MaxRetryReached";
}
