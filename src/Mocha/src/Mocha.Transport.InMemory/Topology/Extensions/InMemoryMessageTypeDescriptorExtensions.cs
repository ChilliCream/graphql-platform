namespace Mocha.Transport.InMemory;

/// <summary>
/// Extension methods for routing outbound messages to specific in-memory queues or topics.
/// </summary>
public static class InMemoryMessageTypeDescriptorExtensions
{
    /// <summary>
    /// Routes the outbound message to an in-memory queue using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema that identifies the in-memory transport (e.g., "memory").</param>
    /// <param name="queueName">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToInMemoryQueue(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string queueName)
        => descriptor.Destination(new Uri($"{schema}:q/{queueName}"));

    /// <summary>
    /// Routes the outbound message to an in-memory queue using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="queueName">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToInMemoryQueue(this IOutboundRouteDescriptor descriptor, string queueName)
        => descriptor.ToInMemoryQueue(InMemoryTransportConfiguration.DefaultSchema, queueName);

    /// <summary>
    /// Routes the outbound message to an in-memory topic using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema that identifies the in-memory transport (e.g., "memory").</param>
    /// <param name="topicName">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToInMemoryTopic(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string topicName)
        => descriptor.Destination(new Uri($"{schema}:t/{topicName}"));

    /// <summary>
    /// Routes the outbound message to an in-memory topic using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="topicName">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToInMemoryTopic(this IOutboundRouteDescriptor descriptor, string topicName)
        => descriptor.ToInMemoryTopic(InMemoryTransportConfiguration.DefaultSchema, topicName);
}
