using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Concrete implementation of <see cref="IMessagingRuntime"/> that holds the fully configured state
/// of the messaging bus, including transports, consumers, routers, and message type registrations.
/// </summary>
/// <remarks>
/// Created once during host startup and shared across all bus operations for the lifetime of the host.
/// Starting the runtime starts all registered transports and their receive endpoints in sequence.
/// </remarks>
/// <param name="services">The root service provider for the messaging host.</param>
/// <param name="options">Read-only messaging configuration options.</param>
/// <param name="naming">Naming conventions used to derive queue, exchange, and endpoint names.</param>
/// <param name="conventions">Registry of conventions applied during configuration and routing.</param>
/// <param name="consumers">The set of all registered consumer definitions.</param>
/// <param name="transports">The ordered list of configured transports (e.g., RabbitMQ, in-memory).</param>
/// <param name="messages">Registry that maps CLR types to <see cref="MessageType"/> metadata.</param>
/// <param name="host">Information about the current host instance (machine name, process ID, etc.).</param>
/// <param name="router">Router that resolves outbound message types to dispatch endpoints.</param>
/// <param name="endpointRouter">Router that resolves or creates dispatch endpoints by URI address.</param>
/// <param name="features">Feature collection shared across the runtime scope.</param>
public sealed class MessagingRuntime(
    IServiceProvider services,
    IReadOnlyMessagingOptions options,
    IBusNamingConventions naming,
    IConventionRegistry conventions,
    ImmutableHashSet<Consumer> consumers,
    ImmutableArray<MessagingTransport> transports,
    IMessageTypeRegistry messages,
    IHostInfo host,
    IMessageRouter router,
    IEndpointRouter endpointRouter,
    IFeatureCollection features) : IMessagingRuntime, IAsyncDisposable
{
    /// <inheritdoc />
    public IServiceProvider Services => services;

    /// <inheritdoc />
    public IBusNamingConventions Naming => naming;

    /// <inheritdoc />
    public IMessageTypeRegistry Messages => messages;

    /// <inheritdoc />
    public IMessageRouter Router => router;

    /// <inheritdoc />
    public IEndpointRouter Endpoints => endpointRouter;

    /// <inheritdoc />
    public IHostInfo Host => host;

    /// <inheritdoc />
    public IConventionRegistry Conventions => conventions;

    /// <inheritdoc />
    public ImmutableHashSet<Consumer> Consumers => consumers;

    /// <inheritdoc />
    public ImmutableArray<MessagingTransport> Transports => transports;

    /// <inheritdoc />
    public IFeatureCollection Features => features;

    /// <inheritdoc />
    public IReadOnlyMessagingOptions Options => options;

    /// <inheritdoc />
    public IMessageBusTopology Topology => field ??= new MessageBusTopology(this);

    /// <inheritdoc />
    public DispatchEndpoint GetSendEndpoint(MessageType messageType)
    {
        return router.GetEndpoint(this, messageType, OutboundRouteKind.Send);
    }

    /// <inheritdoc />
    public DispatchEndpoint GetPublishEndpoint(MessageType messageType)
    {
        return router.GetEndpoint(this, messageType, OutboundRouteKind.Publish);
    }

    /// <inheritdoc />
    public DispatchEndpoint GetDispatchEndpoint(Uri address)
    {
        return endpointRouter.GetOrCreate(this, address);
    }

    /// <inheritdoc />
    public MessageType GetMessageType(Type type)
    {
        return messages.GetOrAdd(this, type);
    }

    /// <inheritdoc />
    public MessageType? GetMessageType(string? identity)
    {
        return identity is not null ? messages.GetMessageType(identity) : null;
    }

    /// <inheritdoc />
    public MessagingTransport? GetTransport(Uri address)
    {
        return transports.FirstOrDefault(t => t.Schema == address.Scheme);
    }

    /// <summary>
    /// Indicates whether all transports have been started and the runtime is accepting operations.
    /// </summary>
    public bool IsStarted { get; private set; }

    /// <summary>
    /// Starts all registered transports and their receive endpoints, enabling message consumption.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the startup sequence.</param>
    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        if (IsStarted)
        {
            return;
        }

        foreach (var transport in transports)
        {
            await transport.StartAsync(this, cancellationToken);
        }

        IsStarted = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var consumer in consumers)
        {
            await consumer.DisposeAsync();
        }
    }
}
