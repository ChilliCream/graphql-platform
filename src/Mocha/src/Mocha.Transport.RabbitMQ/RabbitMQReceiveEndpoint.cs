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

        var topology = (RabbitMQMessagingTopology)Transport.Topology;

        Queue =
            topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
            ?? throw new InvalidOperationException("Queue not found");

        Source = Queue;
    }

    private IAsyncDisposable? _consumer;

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not RabbitMQMessagingTransport rabbitMQMessagingTransport)
        {
            throw new InvalidOperationException("Transport is not a RabbitMQMessagingTransport");
        }

        var topology = (RabbitMQMessagingTopology)rabbitMQMessagingTransport.Topology;
        Func<IChannel, CancellationToken, ValueTask>? provisionReceiveTopology =
            Queue.AutoProvision ?? topology.AutoProvision ? ProvisionReceiveTopologyAsync : null;

        _consumer = await rabbitMQMessagingTransport.ConsumerManager.RegisterConsumerAsync(
            Queue.Name,
            ProcessMessageAsync,
            provisionReceiveTopology,
            _maxPrefetch,
            _consumerDispatchConcurrency,
            cancellationToken);
    }

    private async ValueTask ProvisionReceiveTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        var autoProvision = ((RabbitMQMessagingTopology)Transport.Topology).AutoProvision;

        await Queue.ProvisionAsync(channel, cancellationToken);

        foreach (var binding in Queue.Bindings)
        {
            if (!(binding.AutoProvision ?? autoProvision))
            {
                continue;
            }

            if (binding.Source.AutoProvision ?? autoProvision)
            {
                await binding.Source.ProvisionAsync(channel, cancellationToken);
            }

            await binding.ProvisionAsync(channel, cancellationToken);
        }
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
            await _consumer.DisposeAsync();
        }

        _consumer = null;
    }
}
