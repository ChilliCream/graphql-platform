namespace Mocha.Testing;

/// <summary>
/// Represents a message that was observed by the message tracker.
/// </summary>
public sealed record TrackedMessage
{
    /// <summary>
    /// Gets the message payload object.
    /// </summary>
    public required object Message { get; init; }

    /// <summary>
    /// Gets the CLR type of the message.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the kind of dispatch operation (published or sent).
    /// </summary>
    public required MessageDispatchKind DispatchKind { get; init; }

    /// <summary>
    /// Gets the timestamp relative to the start of tracking.
    /// </summary>
    public required TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Gets the exception that occurred during consumption, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the unique identifier of the message.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets the correlation identifier of the message.
    /// </summary>
    public string? CorrelationId { get; init; }
}
