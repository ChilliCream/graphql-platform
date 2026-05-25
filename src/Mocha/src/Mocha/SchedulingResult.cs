namespace Mocha;

/// <summary>
/// Represents the result of a scheduling operation, containing the opaque cancellation token
/// and metadata about the scheduled message.
/// </summary>
public sealed record SchedulingResult
{
    /// <summary>
    /// Gets the opaque token for cancelling this message, or <c>null</c> if the scheduling
    /// path does not support cancellation.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Gets the time at which the message is scheduled for delivery.
    /// </summary>
    public DateTimeOffset ScheduledTime { get; init; }

    /// <summary>
    /// Gets a value indicating whether the scheduled message can be cancelled via the token.
    /// </summary>
    public bool IsCancellable { get; init; }
}
