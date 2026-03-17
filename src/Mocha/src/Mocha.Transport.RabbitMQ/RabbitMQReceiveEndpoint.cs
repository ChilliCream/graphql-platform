using Mocha.Features;
using Mocha.Transport.RabbitMQ.Features;

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
        _consumerDispatchConcurrency = (ushort)Math.Clamp(configuration.MaxConcurrency ?? 1, 1, (int)ushort.MaxValue);
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

        _consumer = await rabbitMQMessagingTransport.ConsumerManager.RegisterConsumerAsync(
            Queue.Name,
            (channel, eventArgs, ct) =>
                ExecuteAsync(
                    static (context, state) =>
                    {
                        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
                        feature.Channel = state.channel;
                        feature.EventArgs = state.eventArgs;
                    },
                    (channel, eventArgs),
                    ct),
            _maxPrefetch,
            _consumerDispatchConcurrency,
            cancellationToken);
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
