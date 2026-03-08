namespace Mocha.Inbox;

/// <summary>
/// Represents a processed message recorded in the inbox to prevent duplicate processing.
/// </summary>
/// <param name="messageId">The unique message identifier.</param>
/// <param name="consumerType">The consumer type name that processed this message.</param>
/// <param name="messageType">The message type for diagnostics.</param>
/// <param name="processedAt">The UTC timestamp when this message was processed.</param>
public sealed class InboxMessage(string messageId, string consumerType, string? messageType, DateTime processedAt)
{
    /// <summary>
    /// Gets the unique message identifier.
    /// </summary>
    public string MessageId { get; private set; } = messageId;

    /// <summary>
    /// Gets the consumer type name that processed this message.
    /// </summary>
    public string ConsumerType { get; private set; } = consumerType;

    /// <summary>
    /// Gets the message type for diagnostics.
    /// </summary>
    public string? MessageType { get; private set; } = messageType;

    /// <summary>
    /// Gets the UTC timestamp when this message was processed.
    /// </summary>
    public DateTime ProcessedAt { get; private set; } = processedAt;

    // needed for EF Core
    // ReSharper disable once UnusedMember.Local
    private InboxMessage() : this(default!, default!, default, default)
    {
    }
}
