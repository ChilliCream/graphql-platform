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
        _consumerDispatchConcurrency = (ushort)
            Math.Clamp(
                configuration.MaxConcurrency ?? ReceiveEndpointConfiguration.Defaults.MaxConcurrency,
                1,
                ushort.MaxValue);
    }

    protected override void OnDiscoverTopology(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (RabbitMQMessagingTopology)Transport.Topology;

        topology.AddQueue(
            new RabbitMQQueueConfiguration
            {
                Name = configuration.QueueName,
                AutoDelete = Kind == ReceiveEndpointKind.Reply,
                AutoProvision = configuration.AutoProvision,
                Provenance = RabbitMQTopologyProvenance.Endpoint
            });

        // Materialize queue-level BindFrom intents into declared exchange-to-queue bindings. Each
        // intent names a source exchange and an optional routing key; the exchange is ensured in the
        // topology so the binding can reference it.
        var resolver = ((RabbitMQMessagingTransport)Transport).Resolver;
        foreach (var intent in configuration.QueueBindFroms)
        {
            MaterializeBindFrom(topology, resolver, configuration.QueueName, intent);
        }

        // Materialize per-type BindFrom intents. A per-type BindFrom implies AutoBind(false) for
        // that type (handled by the topology convention); here only the explicit bindings are added.
        foreach (var typeBind in configuration.TypeBinds.Values)
        {
            foreach (var intent in typeBind.BindFroms)
            {
                MaterializeBindFrom(topology, resolver, configuration.QueueName, intent);
            }
        }
    }

    private static void MaterializeBindFrom(
        RabbitMQMessagingTopology topology,
        RabbitMQDestinationResolver resolver,
        string queueName,
        BindFromIntent intent)
    {
        if (!resolver.TryResolveSourceExchange(intent.Source, out var exchangeName))
        {
            throw new InvalidOperationException(
                $"BindFrom source '{intent.Source}' could not be resolved to a RabbitMQ exchange name.");
        }

        // Ensure the source exchange exists in the topology with declared provenance. AddExchange
        // merges on duplicate names, so a convention-created entry is upgraded to declared provenance
        // when the user names the same exchange in a BindFrom.
        topology.AddExchange(
            new RabbitMQExchangeConfiguration
            {
                Name = exchangeName,
                Provenance = RabbitMQTopologyProvenance.Declared,
                AutoProvision = topology.AutoProvision
            });

        // Add the exchange-to-queue binding. AddBinding deduplicates by (source, routingKey, args,
        // destination), so repeating the same intent is a safe no-op.
        topology.AddBinding(
            new RabbitMQBindingConfiguration
            {
                Source = exchangeName,
                Destination = queueName,
                DestinationKind = RabbitMQDestinationKind.Queue,
                RoutingKey = intent.RoutingKey,
                Provenance = RabbitMQTopologyProvenance.Declared,
                AutoProvision = topology.AutoProvision
            });
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
