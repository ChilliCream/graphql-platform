namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent builder for configuring a PostgreSQL queue that composes a topology queue descriptor with
/// a lazily created receive endpoint. Infra-only usage (no consumer or routing method called)
/// produces a declared queue in the topology without materializing a receive endpoint.
/// </summary>
public interface IPostgresQueueBuilder
{
    // -- Infra group (delegates to the backing queue descriptor) --

    /// <summary>
    /// Sets whether the backing queue is automatically provisioned in the database.
    /// </summary>
    /// <param name="autoProvision">True to provision the queue; otherwise false.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Sets whether the backing queue is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion; otherwise false.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder AutoDelete(bool autoDelete = true);

    // -- Routing group (delegates to a lazily created receive endpoint) --

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Handler(Type handlerType);

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Consumer(Type consumerType);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Receives<TMessage>();

    /// <summary>
    /// Declares that this queue receives the specified message type with additional binding configuration.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="configure">A delegate to configure per-type binding.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Receives(Type messageType);

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Implicit"/>, enabling
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder BindImplicitly();

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Explicit"/>, suppressing
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder BindExplicitly();

    /// <summary>
    /// Sets the maximum number of messages to fetch per batch on this queue's endpoint.
    /// </summary>
    /// <param name="size">The batch size.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder MaxBatchSize(int size);

    /// <summary>
    /// Sets the kind of receive endpoint for this queue.
    /// </summary>
    /// <param name="kind">The endpoint kind.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on this queue's endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The concurrency limit.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Adds receive middleware to this queue's endpoint pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration.</param>
    /// <param name="before">Optional key to insert the middleware before.</param>
    /// <param name="after">Optional key to insert the middleware after.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the fault endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The fault endpoint address.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder FaultEndpoint(string name);

    /// <summary>
    /// Sets the skipped endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The skipped endpoint address.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder SkippedEndpoint(string name);

    // -- Satellite sugar (calls EnsureEndpoint, writes to satellite config) --

    /// <summary>
    /// Sets the verbatim name of the error queue satellite for this queue's endpoint.
    /// </summary>
    /// <param name="name">The exact queue name to use for the error satellite.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder ErrorQueue(string name);

    /// <summary>
    /// Disables the error queue satellite for this queue's endpoint.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder DisableErrorQueue();

    /// <summary>
    /// Sets the verbatim name of the skipped queue satellite for this queue's endpoint.
    /// </summary>
    /// <param name="name">The exact queue name to use for the skipped satellite.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder SkippedQueue(string name);

    /// <summary>
    /// Disables the skipped queue satellite for this queue's endpoint.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder DisableSkippedQueue();

    // -- BindFrom (infra group, writes directly to topology) --

    /// <summary>
    /// Binds this queue to a source topic, writing the topic and subscription directly to the
    /// transport topology without materializing a receive endpoint.
    /// </summary>
    /// <param name="source">The source URI identifying the topic.</param>
    /// <param name="routingKey">An optional routing key for the binding.</param>
    /// <returns>The builder for method chaining.</returns>
    IPostgresQueueBuilder BindFrom(Uri source, string? routingKey = null);

    // -- Escape hatches --

    /// <summary>
    /// Returns the underlying queue descriptor for direct topology configuration.
    /// </summary>
    /// <returns>The queue descriptor.</returns>
    IPostgresQueueDescriptor AsQueue();

    /// <summary>
    /// Returns the underlying receive endpoint descriptor, creating it if necessary.
    /// </summary>
    /// <returns>The receive endpoint descriptor.</returns>
    IPostgresReceiveEndpointDescriptor AsEndpoint();
}
