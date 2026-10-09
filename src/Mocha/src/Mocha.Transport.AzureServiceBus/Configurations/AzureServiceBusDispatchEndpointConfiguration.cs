namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for an Azure Service Bus dispatch endpoint, specifying the target queue or topic
/// for outbound messages.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="QueueName"/> or <see cref="TopicName"/> should be set.
/// When <see cref="QueueName"/> is set, messages are sent directly to the queue (point-to-point).
/// When <see cref="TopicName"/> is set, messages are published to the topic (fan-out).
/// </remarks>
public sealed class AzureServiceBusDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the target queue name for direct dispatch. Mutually exclusive with <see cref="TopicName"/>.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the target topic name for publish dispatch. Mutually exclusive with <see cref="QueueName"/>.
    /// </summary>
    public string? TopicName { get; set; }
}
