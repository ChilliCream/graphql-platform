using System.Text.Json;

namespace Mocha.Outbox;

/// <summary>
/// Represents a message stored in the SQL Server outbox table, awaiting dispatch by the outbox processor.
/// </summary>
/// <param name="id">The unique identifier for this outbox message.</param>
/// <param name="envelope">The serialized message envelope containing headers, body, and routing information.</param>
public sealed class OutboxMessage(Guid id, JsonDocument envelope)
{
    /// <summary>
    /// Gets the unique identifier for this outbox message.
    /// </summary>
    public Guid Id { get; private set; } = id;

    /// <summary>
    /// Gets the serialized message envelope containing headers, body, and routing information.
    /// </summary>
    public JsonDocument Envelope { get; private set; } = envelope;

    /// <summary>
    /// Gets the number of times the outbox processor has attempted to dispatch this message.
    /// </summary>
    public int TimesSent { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this outbox message was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
