using System.Text.Json;

namespace Mocha.Scheduling;

/// <summary>
/// Represents a message stored in the Postgres scheduled messages table, awaiting dispatch at the scheduled time.
/// </summary>
/// <param name="id">The unique identifier for this scheduled message.</param>
/// <param name="envelope">The serialized message envelope containing headers, body, and routing information.</param>
public sealed class ScheduledMessage(Guid id, JsonDocument envelope)
{
    /// <summary>
    /// Gets the unique identifier for this scheduled message.
    /// </summary>
    public Guid Id { get; private set; } = id;

    /// <summary>
    /// Gets the serialized message envelope containing headers, body, and routing information.
    /// </summary>
    public JsonDocument Envelope { get; private set; } = envelope;

    /// <summary>
    /// Gets the UTC time at which the message should be dispatched.
    /// </summary>
    public DateTime ScheduledTime { get; private set; }

    /// <summary>
    /// Gets the number of times the scheduler has attempted to dispatch this message.
    /// </summary>
    public int TimesSent { get; private set; }

    /// <summary>
    /// Gets the maximum number of times dispatch will be attempted before the message is considered dead.
    /// </summary>
    public int MaxAttempts { get; private set; } = 10;

    /// <summary>
    /// Gets the last error encountered during dispatch, stored as a JSON string.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this scheduled message was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
