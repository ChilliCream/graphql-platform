namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent descriptor for configuring a PostgreSQL queue and its receive endpoint.
/// </summary>
public interface IPostgresQueueDescriptor : IMessagingDescriptor<PostgresQueueDescriptorConfiguration>
{
    /// <summary>
    /// Sets whether the backing queue is automatically provisioned in the database.
    /// </summary>
    /// <param name="autoProvision">True to provision the queue; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Sets whether the backing queue is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Handler(Type handlerType);

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Consumer(Type consumerType);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Receives<TMessage>();

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Receives(Type messageType);

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Implicit"/>, enabling
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor BindImplicitly();

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Explicit"/>, suppressing
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor BindExplicitly();

    /// <summary>
    /// Sets the maximum number of messages to fetch per batch on this queue's endpoint.
    /// </summary>
    /// <param name="size">The batch size.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor MaxBatchSize(int size);

    /// <summary>
    /// Sets the kind of receive endpoint for this queue.
    /// </summary>
    /// <param name="kind">The endpoint kind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on this queue's endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The concurrency limit.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Adds receive middleware to this queue's endpoint pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration.</param>
    /// <param name="before">Optional key to insert the middleware before.</param>
    /// <param name="after">Optional key to insert the middleware after.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the fault endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="address">The fault endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor FaultEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding failed messages to a fault endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor DisableFaultEndpoint();

    /// <summary>
    /// Sets the skipped endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="address">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor SkippedEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding skipped messages to a skipped endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor DisableSkippedEndpoint();

    /// <summary>
    /// Binds this queue to a source topic, writing the topic and subscription directly to the
    /// transport topology.
    /// </summary>
    /// <param name="source">The source URI identifying the topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueDescriptor BindFrom(Uri source);
}
