using Mocha.EntityFrameworkCore;

namespace Mocha.Outbox;

/// <summary>
/// Represents a message stored in the Postgres outbox table, awaiting dispatch by the outbox processor.
/// </summary>
/// <param name="id">The unique identifier for this outbox message.</param>
/// <param name="envelope">The serialized message envelope containing headers, body, and routing information.</param>
public sealed class OutboxMessage(Guid id, string envelope) : IPreparedOutboxMessage
{
    /// <summary>
    /// Gets the unique identifier for this outbox message.
    /// </summary>
    public Guid Id { get; private set; } = id;

    /// <summary>
    /// Gets the serialized message envelope containing headers, body, and routing information.
    /// </summary>
    public string Envelope { get; private set; } = envelope;

    /// <summary>
    /// Gets the number of times the outbox processor has attempted to dispatch this message.
    /// </summary>
    public int TimesSent { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this outbox message was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the identifier of the ordered group, or null for an independently delivered message.
    /// </summary>
    public Guid? GroupId { get; internal set; }

    /// <summary>
    /// Gets the position of the message within its ordered group.
    /// </summary>
    public long Sequence { get; internal set; }

    /// <summary>
    /// Gets the earliest UTC delivery time, or null for immediate delivery after commit.
    /// </summary>
    public DateTime? ScheduledAt { get; internal set; }

    void IDisposable.Dispose() { }
}
