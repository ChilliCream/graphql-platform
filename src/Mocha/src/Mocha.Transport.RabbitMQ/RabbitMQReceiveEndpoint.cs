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

    private RabbitMQConsumerManager.RegisteredConsumer? _consumer;

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not RabbitMQMessagingTransport rabbitMQMessagingTransport)
        {
            throw new InvalidOperationException("Transport is not a RabbitMQMessagingTransport");
        }

        _consumer = await rabbitMQMessagingTransport.ConsumerManager.RegisterConsumerCoreAsync(
            Queue.Name,
            ProcessMessageAsync,
            _maxPrefetch,
            _consumerDispatchConcurrency,
            ProcessingToken,
            cancellationToken);
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
            // The acknowledgement middleware requeues the message while the channel is open, and
            // closing the channel requeues it otherwise.
        }
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        var consumer = _consumer;
        _consumer = null;
        if (consumer is not null)
        {
            var completed = false;
            try
            {
                completed = await WaitForProcessingAsync(consumer.StopReceivingAsync(), cancellationToken);
            }
            finally
            {
                // Closing the channel requeues the messages of handlers that outlast the stop.
                if (completed || !transport.TryDeferConsumerCleanup(consumer))
                {
                    await consumer.DisposeAsync();
                }
            }
        }
    }
}
