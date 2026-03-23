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
    /// Adds a dispatch middleware configuration to the dispatch pipeline.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    IMessageBusBuilder UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Adds a receive middleware configuration to the receive pipeline.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    IMessageBusBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Adds a consumer middleware configuration to the consumer pipeline.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    IMessageBusBuilder UseConsume(
        ConsumerMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
