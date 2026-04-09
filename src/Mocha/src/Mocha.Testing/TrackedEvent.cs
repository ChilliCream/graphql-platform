namespace Mocha.Testing;

/// <summary>
/// Represents a single event in the message tracking timeline.
/// </summary>
public sealed record TrackedEvent
{
    /// <summary>
    /// Gets the kind of event that occurred.
    /// </summary>
    public required TrackedEventKind Kind { get; init; }

    /// <summary>
    /// Gets the CLR type of the message involved in this event.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the timestamp relative to the start of tracking.
    /// </summary>
    public required TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Gets the unique identifier of the message, if available.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets the duration of the operation, if applicable.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; init; }
}
