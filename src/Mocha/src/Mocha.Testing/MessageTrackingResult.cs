namespace Mocha.Testing;

/// <summary>
/// Represents the result of waiting for message tracking completion,
/// containing all tracked messages and timing information.
/// </summary>
public sealed class MessageTrackingResult : ITrackedMessages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTrackingResult"/> class.
    /// </summary>
    public MessageTrackingResult(
        IReadOnlyList<TrackedMessage> dispatched,
        IReadOnlyList<TrackedMessage> consumed,
        IReadOnlyList<TrackedMessage> failed,
        bool completed,
        TimeSpan elapsed)
    {
        Dispatched = dispatched;
        Consumed = consumed;
        Failed = failed;
        Completed = completed;
        Elapsed = elapsed;
    }

    /// <inheritdoc />
    public IReadOnlyList<TrackedMessage> Dispatched { get; }

    /// <inheritdoc />
    public IReadOnlyList<TrackedMessage> Consumed { get; }

    /// <inheritdoc />
    public IReadOnlyList<TrackedMessage> Failed { get; }

    /// <summary>
    /// Gets a value indicating whether all dispatched messages were consumed or failed
    /// before the timeout elapsed.
    /// </summary>
    public bool Completed { get; }

    /// <summary>
    /// Gets the total elapsed time from the start of the wait operation.
    /// </summary>
    public TimeSpan Elapsed { get; }
}
