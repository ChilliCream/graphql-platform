namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring an in-memory binding topology entity.
/// </summary>
public interface IInMemoryBindingTopologyDescriptor : IMessagingDescriptor<InMemoryBindingConfiguration>
{
    /// <summary>
    /// Sets the source topic name.
    /// </summary>
    /// <param name="topicName">The name of the topic from which messages will be routed.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryBindingTopologyDescriptor Source(string topicName);

    /// <summary>
    /// Sets the destination queue or topic name.
    /// </summary>
    /// <param name="queueName">The name of the queue where messages will be routed.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryBindingTopologyDescriptor ToQueue(string queueName);

    /// <summary>
    /// Sets the destination topic name.
    /// </summary>
    /// <param name="topicName">The name of the topic where messages will be routed.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryBindingTopologyDescriptor ToTopic(string topicName);
}
