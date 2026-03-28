using NATS.Client.Core;

namespace Mocha.Transport.NATS;

/// <summary>
/// Fluent interface for configuring a NATS JetStream messaging transport, including connection, topology, endpoints, and middleware.
/// </summary>
public interface INatsMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<NatsTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions" />
    new INatsMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema" />
    new INatsMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersImplicitly" />
    new INatsMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersExplicitly" />
    new INatsMessagingTransportDescriptor BindHandlersExplicitly();

    /// <summary>
    /// Sets the NATS server URL for connection (e.g., "nats://localhost:4222").
    /// </summary>
    /// <param name="url">The NATS server URL.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor Url(string url);

    /// <summary>
    /// Sets a factory delegate that resolves a <see cref="NatsConnection"/> from the service provider.
    /// </summary>
    /// <param name="connectionFactory">A factory that takes an <see cref="IServiceProvider"/> and returns a connection.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor ConnectionFactory(
        Func<IServiceProvider, NatsConnection> connectionFactory);

    /// <summary>
    /// Configures bus-level defaults that are applied to all auto-provisioned streams and consumers.
    /// </summary>
    /// <param name="configure">A delegate that configures the bus defaults.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor ConfigureDefaults(Action<NatsBusDefaults> configure);

    /// <summary>
    /// Gets or creates a receive endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, also used as the default subject and consumer name.</param>
    /// <returns>A receive endpoint descriptor for further configuration.</returns>
    INatsReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Gets or creates a dispatch endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, also used as the default subject name.</param>
    /// <returns>A dispatch endpoint descriptor for further configuration.</returns>
    INatsDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Declares or retrieves a stream in the transport topology.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <returns>A stream descriptor for further configuration.</returns>
    INatsStreamDescriptor DeclareStream(string name);

    /// <summary>
    /// Declares or retrieves a consumer in the transport topology.
    /// </summary>
    /// <param name="name">The consumer name.</param>
    /// <returns>A consumer descriptor for further configuration.</returns>
    INatsConsumerDescriptor DeclareConsumer(string name);

    /// <summary>
    /// Sets whether topology resources should be automatically provisioned on the broker.
    /// When disabled, streams and consumers must exist before the transport starts.
    /// Individual resources can override this setting via their own <c>AutoProvision</c> method.
    /// </summary>
    /// <param name="autoProvision">
    /// <c>true</c> to enable auto-provisioning (default); <c>false</c> to disable it globally.
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor AutoProvision(bool autoProvision = true);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name" />
    new INatsMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention" />
    new INatsMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport" />
    new INatsMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch" />
    new INatsMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AppendDispatch" />
    new INatsMessagingTransportDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.PrependDispatch" />
    new INatsMessagingTransportDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive" />
    new INatsMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AppendReceive" />
    new INatsMessagingTransportDescriptor AppendReceive(string after, ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.PrependReceive" />
    new INatsMessagingTransportDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration);
}
