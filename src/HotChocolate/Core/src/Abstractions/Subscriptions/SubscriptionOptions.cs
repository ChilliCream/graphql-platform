namespace HotChocolate.Subscriptions;

public class SubscriptionOptions
{
    /// <summary>
    /// Specifies the topic prefix.
    /// </summary>
    public string? TopicPrefix { get; set; }

    /// <summary>
    /// Specifies the in-memory buffer size for messages per topic.
    /// </summary>
    public int TopicBufferCapacity { get; set; } = 64;

    /// <summary>
    /// Specifies the behavior to use when writing to a topic buffer that is already full.
    /// </summary>
    public TopicBufferFullMode TopicBufferFullMode { get; set; } = TopicBufferFullMode.Wait;
}
