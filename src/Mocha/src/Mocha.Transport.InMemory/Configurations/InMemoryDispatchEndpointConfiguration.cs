namespace Mocha.Transport.InMemory;

/// <summary>
/// Configuration for a dispatch endpoint targeting an in-memory queue or topic.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="QueueName"/> or <see cref="TopicName"/> should be set.
/// When <see cref="QueueName"/> is set the endpoint dispatches directly to a queue;
/// when <see cref="TopicName"/> is set the endpoint publishes through a topic.
/// </remarks>
public sealed class InMemoryDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the name of the target queue, or <c>null</c> when the endpoint dispatches to a topic.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the name of the target topic, or <c>null</c> when the endpoint dispatches to a queue.
    /// </summary>
    public string? TopicName { get; set; }
}
