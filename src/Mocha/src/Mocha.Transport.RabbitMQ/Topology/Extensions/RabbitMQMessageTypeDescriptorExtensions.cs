namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for configuring outbound route destinations targeting RabbitMQ queues and exchanges.
/// </summary>
public static class RabbitMQMessageTypeDescriptorExtensions
{
    /// <summary>
    /// Sets the outbound route destination to a RabbitMQ queue using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema for the transport (e.g., "rabbitmq").</param>
    /// <param name="queueName">The target queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToRabbitMQQueue(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string queueName)
        => descriptor.Destination(new Uri($"{schema}:q/{queueName}"));

    /// <summary>
    /// Sets the outbound route destination to a RabbitMQ queue using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="queueName">The target queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToRabbitMQQueue(this IOutboundRouteDescriptor descriptor, string queueName)
        => descriptor.ToRabbitMQQueue(RabbitMQTransportConfiguration.DefaultSchema, queueName);

    /// <summary>
    /// Sets the outbound route destination to a RabbitMQ exchange using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema for the transport (e.g., "rabbitmq").</param>
    /// <param name="exchangeName">The target exchange name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToRabbitMQExchange(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string exchangeName)
        => descriptor.Destination(new Uri($"{schema}:e/{exchangeName}"));

    /// <summary>
    /// Sets the outbound route destination to a RabbitMQ exchange using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="exchangeName">The target exchange name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToRabbitMQExchange(
        this IOutboundRouteDescriptor descriptor,
        string exchangeName)
        => descriptor.ToRabbitMQExchange(RabbitMQTransportConfiguration.DefaultSchema, exchangeName);
}
