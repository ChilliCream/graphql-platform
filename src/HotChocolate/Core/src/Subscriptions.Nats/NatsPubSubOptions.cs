namespace HotChocolate.Subscriptions.Nats;

public sealed class NatsPubSubOptions
{
    /// <summary>
    /// Specifies the topic prefix.
    /// </summary>
    public string? TopicPrefix { get; set; }

    /// <summary>
    /// Specifies the in-memory buffer size for messages per topic.
    /// </summary>
    public int TopicBufferCapacity { get; set; } = 20;

    /// <summary>
    /// Specifies the behavior to use when writing to a topic buffer that is already full.
    /// </summary>
    public NatsTopicBufferFullMode TopicBufferFullMode { get; set; } = NatsTopicBufferFullMode.Wait;
}

/// <summary>
/// Specifies the behavior to use when writing to a topic buffer that is already full.
/// </summary>
public enum NatsTopicBufferFullMode
{
    /// <summary>
    /// Wait for space to be available in order to complete the write operation.
    /// </summary>
    Wait,

    /// <summary>
    /// Remove and ignore the newest item in the channel in order to make room for
    /// the item being written.
    /// </summary>
    DropNewest,

    /// <summary>
    /// Remove and ignore the oldest item in the channel in order to make room for
    /// the item being written.
    /// </summary>
    DropOldest,

    /// <summary>
    /// Drop the item being written.
    /// </summary>
    DropWrite
}
