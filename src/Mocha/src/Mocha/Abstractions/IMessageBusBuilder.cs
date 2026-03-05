using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Provides a fluent API for configuring message handlers, sagas, transports, middleware pipelines,
/// and message types before building the messaging runtime.
/// </summary>
public interface IMessageBusBuilder
{
    /// <summary>
    /// Registers a message handler that will consume messages matching its declared message type.
    /// </summary>
    /// <typeparam name="THandler">The handler type implementing <see cref="IHandler"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AddHandler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a batch event handler that collects messages and delivers them in batches.
    /// </summary>
    /// <typeparam name="THandler">The batch handler type.</typeparam>
    /// <param name="configure">Optional action to configure batch options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AddBatchHandler<THandler>(Action<BatchOptions>? configure = null)
        where THandler : class, IBatchEventHandler;

    /// <summary>
    /// Registers a saga state machine that coordinates long-running workflows across multiple
    /// message exchanges.
    /// </summary>
    /// <typeparam name="TSaga">The saga type deriving from <see cref="Saga"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AddSaga<TSaga>() where TSaga : Saga, new();

    /// <summary>
    /// Adds a messaging transport (e.g., RabbitMQ, Azure Service Bus) to the bus configuration.
    /// </summary>
    /// <typeparam name="TTransport">
    /// The transport type deriving from <see cref="MessagingTransport"/>.
    /// </typeparam>
    /// <param name="transport">The transport instance to register.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AddTransport<TTransport>(TTransport transport) where TTransport : MessagingTransport;

    /// <summary>
    /// Modifies the global messaging options that control bus-wide behavior such as timeouts and
    /// naming.
    /// </summary>
    /// <param name="configure">An action to apply changes to the messaging options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder ModifyOptions(Action<MessagingOptions> configure);

    /// <summary>
    /// Registers additional services into the dependency injection container used by the message
    /// bus.
    /// </summary>
    /// <param name="configure">An action to register services.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    /// Registers additional services into the dependency injection container, with access to the
    /// existing service provider for conditional registration.
    /// </summary>
    /// <param name="configure">An action receiving the current service provider and the service collection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder ConfigureServices(Action<IServiceProvider, IServiceCollection> configure);

    /// <summary>
    /// Configures the bus-level feature collection, allowing middleware and extensions to store
    /// cross-cutting state.
    /// </summary>
    /// <param name="configure">An action to modify the feature collection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder ConfigureFeature(Action<IFeatureCollection> configure);

    /// <summary>
    /// Configures the host information for this message bus instance.
    /// </summary>
    /// <param name="configure">An action to configure the host information.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder Host(Action<IHostInfoDescriptor> configure);

    /// <summary>
    /// Registers a message type with the bus and configures its serialization, routing, and metadata.
    /// </summary>
    /// <typeparam name="TMessage">The CLR type of the message.</typeparam>
    /// <param name="configure">An action to configure the message type descriptor.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AddMessage<TMessage>(Action<IMessageTypeDescriptor> configure) where TMessage : class;

    /// <summary>
    /// Builds and returns the fully configured <see cref="MessagingRuntime"/> using the specified
    /// service provider.
    /// </summary>
    /// <param name="services">
    /// The application-level service provider used to resolve runtime dependencies.
    /// </param>
    /// <returns>The constructed messaging runtime ready to start.</returns>
    MessagingRuntime Build(IServiceProvider services);

    /// <summary>
    /// Appends a dispatch middleware configuration to the end of the dispatch pipeline.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a dispatch middleware configuration immediately after the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="after">The name of the existing middleware after which to insert.</param>
    /// <param name="configuration">The dispatch middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AppendDispatch(string after, DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a dispatch middleware configuration immediately before the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="before">The name of the existing middleware before which to insert.</param>
    /// <param name="configuration">The dispatch middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder PrependDispatch(string before, DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Appends a dispatch middleware configuration to the end of the current dispatch pipeline.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AppendDispatch(DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Prepends a dispatch middleware configuration to the beginning of the current dispatch
    /// pipeline.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to prepend.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder PrependDispatch(DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Appends a receive middleware configuration to the end of the receive pipeline.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder UseReceive(ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a receive middleware configuration immediately after the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="after">The name of the existing middleware after which to insert.</param>
    /// <param name="configuration">The receive middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AppendReceive(string after, ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a receive middleware configuration immediately before the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="before">The name of the existing middleware before which to insert.</param>
    /// <param name="configuration">The receive middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder PrependReceive(string before, ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Appends a receive middleware configuration to the end of the current receive pipeline.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AppendReceive(ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Prepends a receive middleware configuration to the beginning of the current receive
    /// pipeline.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to prepend.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder PrependReceive(ReceiveMiddlewareConfiguration configuration);

    /// <summary>
    /// Appends a consumer middleware configuration to the end of the consumer pipeline.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder UseConsume(ConsumerMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a consumer middleware configuration immediately after the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="after">The name of the existing middleware after which to insert.</param>
    /// <param name="configuration">The consumer middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AppendConsume(string after, ConsumerMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a consumer middleware configuration immediately before the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="before">The name of the existing middleware before which to insert.</param>
    /// <param name="configuration">The consumer middleware configuration to insert.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder PrependConsume(string before, ConsumerMiddlewareConfiguration configuration);

    /// <summary>
    /// Appends a consumer middleware configuration to the end of the current consumer pipeline.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to append.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder AppendConsume(ConsumerMiddlewareConfiguration configuration);

    /// <summary>
    /// Prepends a consumer middleware configuration to the beginning of the current consumer
    /// pipeline.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to prepend.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IMessageBusBuilder PrependConsume(ConsumerMiddlewareConfiguration configuration);
}
