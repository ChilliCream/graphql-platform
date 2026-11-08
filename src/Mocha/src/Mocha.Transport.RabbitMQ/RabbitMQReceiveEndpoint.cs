using System.Collections.Immutable;
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
    /// Gets whether the endpoint is attached and has capacity available beyond handlers cancelled by a channel loss.
    /// </summary>
    public bool IsHealthy => _consumer is { IsHealthy: true };

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

    private RabbitMQConsumerManager.RegisteredConsumer? _consumer;

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
        var dependencies = ImmutableArray<Func<RabbitMQConsumerManager.RegisteredConsumer?>>.Empty;
        if (Kind is not ReceiveEndpointKind.Reply
            && transport.ReplyReceiveEndpoint is RabbitMQReceiveEndpoint replyEndpoint)
        {
            await replyEndpoint.StartAsync(context, cancellationToken);
            dependencies = [() => replyEndpoint._consumer];
        }

        _consumer = await transport.ConsumerManager.RegisterConsumerAsync(
            Queue.Name,
            ProvisionAsync,
            dependencies,
            ProcessMessageAsync,
            _maxPrefetch,
            _consumerDispatchConcurrency,
            cancellationToken);

        Task ProvisionAsync(IConnection connection, IChannel channel, CancellationToken ct)
            => new RabbitMQTopologyProvisioner(transport.Topology, connection).ProvisionReceiveAsync(Queue, channel, ct);
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

        // A generated temporary reply queue is deleted on graceful stop when auto-provisioning is enabled.
        if (Kind == ReceiveEndpointKind.Reply
            && Configuration.IsTemporary
            && Queue.Origin is not TopologyOrigin.Declared
            && (Queue.AutoProvision ?? transport.Topology.AutoProvision))
        {
            await transport.ConsumerManager.DeleteQueueAsync(Queue.Name, cancellationToken);
        }
    }
}
