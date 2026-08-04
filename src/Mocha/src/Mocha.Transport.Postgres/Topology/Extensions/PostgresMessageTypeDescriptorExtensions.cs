namespace Mocha.Transport.Postgres;

/// <summary>
/// Extension methods for routing outbound messages to specific PostgreSQL queues or topics.
/// </summary>
public static class PostgresMessageTypeDescriptorExtensions
{
    /// <summary>
    /// Routes the outbound message to a PostgreSQL queue using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema that identifies the PostgreSQL transport (e.g., "postgres").</param>
    /// <param name="queueName">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToPostgresQueue(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string queueName)
        => descriptor.Destination(new Uri($"{schema}:q/{queueName}"));

    /// <summary>
    /// Routes the outbound message to a PostgreSQL queue using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="queueName">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToPostgresQueue(this IOutboundRouteDescriptor descriptor, string queueName)
        => descriptor.ToPostgresQueue(PostgresTransportConfiguration.DefaultSchema, queueName);

    /// <summary>
    /// Routes the outbound message to a PostgreSQL topic using the specified schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="schema">The URI schema that identifies the PostgreSQL transport (e.g., "postgres").</param>
    /// <param name="topicName">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToPostgresTopic(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string topicName)
        => descriptor.Destination(new Uri($"{schema}:t/{topicName}"));

    /// <summary>
    /// Routes the outbound message to a PostgreSQL topic using the default schema.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor to configure.</param>
    /// <param name="topicName">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IOutboundRouteDescriptor ToPostgresTopic(this IOutboundRouteDescriptor descriptor, string topicName)
        => descriptor.ToPostgresTopic(PostgresTransportConfiguration.DefaultSchema, topicName);
}
