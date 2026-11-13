using Mocha.Transport.RabbitMQ.Features;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// RabbitMQ receive endpoint that consumes messages from a specific queue using the transport's consumer manager.
/// </summary>
/// <param name="transport">The owning RabbitMQ transport instance.</param>
public sealed class RabbitMQReceiveEndpoint(RabbitMQMessagingTransport transport)
    : ReceiveEndpoint<RabbitMQReceiveEndpointConfiguration>(transport)
{
    private ushort _maxPrefetch = 100;
    private ushort _consumerDispatchConcurrency = 1;

    /// <summary>
    /// Gets the RabbitMQ queue that this endpoint consumes from.
    /// </summary>
    public RabbitMQQueue Queue { get; private set; } = null!;

    /// <summary>
    /// Gets whether the endpoint's consumer is attached and the broker delivers to it.
    /// </summary>
    internal bool IsHealthy => _consumer is { IsAttached: true };

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        _maxPrefetch = configuration.MaxPrefetch;
        _consumerDispatchConcurrency = (ushort)
            Math.Clamp(
                configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency,
                1,
                ushort.MaxValue);
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = transport.Topology;

        Queue =
            topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw new InvalidOperationException("Queue not found");

        Source = Queue;
    }

    private RabbitMQConsumer? _consumer;

    /// <summary>
    /// Waits until the endpoint's consumer is attached. Fails when the endpoint is not started.
    /// </summary>
    internal ValueTask WaitUntilAttachedAsync(CancellationToken cancellationToken)
    {
        if (_consumer is not { } consumer)
        {
            throw ThrowHelper.ReplyEndpointNotStarted();
        }

        return consumer.WaitUntilAttachedAsync(cancellationToken);
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        _consumer = await transport.ConsumerManager.RegisterConsumerAsync(
            Queue.Name,
            PrepareChannelAsync,
            ProcessMessageAsync,
            _maxPrefetch,
            _consumerDispatchConcurrency,
            cancellationToken);

        Task PrepareChannelAsync(IConnection connection, IChannel channel, CancellationToken ct)
            => new RabbitMQTopologyProvisioner(transport.Topology, connection).ProvisionQueueAsync(Queue, channel, ct);
    }

    private async ValueTask ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(
                static (context, state) =>
                {
                    var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
                    feature.Channel = state.channel;
                    feature.EventArgs = state.eventArgs;
                },
                (channel, eventArgs),
                cancellationToken);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Message processing was cancelled during shutdown.
        }
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_consumer is not null)
        {
            await _consumer.StopAsync(cancellationToken);
        }

        _consumer = null;

        // A generated temporary reply queue is deleted when auto-provisioning is enabled, also when the token
        // is cancelled.
        if (Kind == ReceiveEndpointKind.Reply
            && Configuration.IsTemporary
            && Queue.Origin is not TopologyOrigin.Declared
            && transport.Topology.ShouldProvision(Queue))
        {
            await transport.ConsumerManager.DeleteQueueAsync(Queue.Name);
        }
    }
}
