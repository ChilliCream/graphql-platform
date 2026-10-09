namespace Mocha;

/// <summary>
/// Represents the consume context for a batch of messages.
/// </summary>
public interface IBatchConsumeContext : IConsumeContext<IMessageBatch>
{
    /// <summary>
    /// Gets the logical identifier of this batch operation.
    /// </summary>
    string BatchId { get; }

    /// <summary>
    /// Gets the message type metadata for each item in the batch.
    /// </summary>
    MessageType? ItemMessageType { get; }
}
