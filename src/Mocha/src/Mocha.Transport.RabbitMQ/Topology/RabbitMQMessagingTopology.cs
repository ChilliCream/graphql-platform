namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages the RabbitMQ topology model (exchanges, queues, and bindings) for a transport instance,
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
public sealed class RabbitMQMessagingTopology(
    RabbitMQMessagingTransport transport,
    Uri baseAddress,
    RabbitMQBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<RabbitMQMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<RabbitMQExchange> _exchanges = [];
    private readonly List<RabbitMQQueue> _queues = [];
    private readonly List<RabbitMQBinding> _bindings = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets the list of exchanges registered in this topology.
    /// </summary>
    public IReadOnlyList<RabbitMQExchange> Exchanges => _exchanges;

    /// <summary>
    /// Gets the list of queues registered in this topology.
    /// </summary>
    public IReadOnlyList<RabbitMQQueue> Queues => _queues;

    /// <summary>
    /// Gets the list of bindings connecting exchanges to queues or other exchanges in this topology.
    /// </summary>
    public IReadOnlyList<RabbitMQBinding> Bindings => _bindings;

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned queues and exchanges.
    /// </summary>
    public RabbitMQBusDefaults Defaults => defaults;

    /// <summary>
    /// Adds an exchange to the topology or merges into an existing exchange with the same name.
    /// When an exchange with the same name already exists, the incoming configuration is merged
    /// using the 3.5 rules: declared non-null scalar wins, convention fills the rest, Arguments
    /// union per key, AutoProvision strengthens (true wins), provenance upgrades convention to
    /// endpoint to declared. A shape conflict between two declared values throws
    /// <see cref="RabbitMQTopologyShapeConflictException"/>.
    /// </summary>
    /// <param name="configuration">The exchange configuration specifying name, type, durability, and arguments.</param>
    /// <returns>The created or merged exchange resource.</returns>
    /// <exception cref="RabbitMQTopologyShapeConflictException">
    /// Thrown when both the existing and incoming configurations carry explicitly declared values
    /// for the same scalar property and those values differ.
    /// </exception>
    public RabbitMQExchange AddExchange(RabbitMQExchangeConfiguration configuration)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == configuration.Name);
            if (exchange is not null)
            {
                exchange.MergeFrom(configuration);
                return exchange;
            }

            exchange = new RabbitMQExchange();

            configuration.Topology = this;
            defaults.Exchange.ApplyTo(configuration);
            exchange.Initialize(configuration);

            _exchanges.Add(exchange);

            exchange.Complete();

            return exchange;
        }
    }

    /// <summary>
    /// Adds a queue to the topology or merges into an existing queue with the same name.
    /// When a queue with the same name already exists, the incoming configuration is merged
    /// using the 3.5 rules: declared non-null scalar wins, convention fills the rest, Arguments
    /// union per key, AutoProvision strengthens (true wins), provenance upgrades convention to
    /// endpoint to declared. A shape conflict between two declared values throws
    /// <see cref="RabbitMQTopologyShapeConflictException"/>.
    /// </summary>
    /// <param name="configuration">The queue configuration specifying name, durability, exclusivity, and arguments.</param>
    /// <returns>The created or merged queue resource.</returns>
    /// <exception cref="RabbitMQTopologyShapeConflictException">
    /// Thrown when both the existing and incoming configurations carry explicitly declared values
    /// for the same scalar property and those values differ.
    /// </exception>
    public RabbitMQQueue AddQueue(RabbitMQQueueConfiguration configuration)
    {
        lock (_lock)
        {
            configuration.Topology ??= this;

            var queue = _queues.FirstOrDefault(q => q.Name == configuration.Name);
            if (queue is not null)
            {
                queue.MergeFrom(configuration);
                return queue;
            }

            configuration.Topology = this;

            defaults.Queue.ApplyTo(configuration);

            queue = new RabbitMQQueue();
            queue.Initialize(configuration);

            _queues.Add(queue);

            queue.Complete();

            return queue;
        }
    }

    /// <summary>
    /// Adds a new binding to the topology, connecting a source exchange to a destination queue or exchange.
    /// </summary>
    /// <param name="configuration">The binding configuration specifying source, destination, routing key, and arguments.</param>
    /// <returns>The created and initialized binding resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source exchange or destination resource is not found in the topology.</exception>
    public RabbitMQBinding AddBinding(RabbitMQBindingConfiguration configuration)
    {
        lock (_lock)
        {
            var routingKey = configuration.RoutingKey ?? string.Empty;
            var arguments = configuration.Arguments?
                .Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value));
            var existing = _bindings.FirstOrDefault(b =>
                b.Source.Name == configuration.Source
                && b.RoutingKey == routingKey
                && RabbitMQBinding.ArgumentsEqual(b.Arguments, arguments)
                && MatchesDestination(b, configuration));
            if (existing is not null)
            {
                // A repeated declaration of the same binding keeps the stronger metadata so an
                // explicit auto-provision or a declared provenance is not lost to an earlier
                // convention-created entry.
                existing.MergeFrom(configuration);
                return existing;
            }

            var source = _exchanges.FirstOrDefault(e => e.Name == configuration.Source);
            if (source is null)
            {
                throw new InvalidOperationException($"Source exchange '{configuration.Source}' not found");
            }

            RabbitMQBinding binding;

            if (configuration.DestinationKind == RabbitMQDestinationKind.Queue)
            {
                var destination = _queues.FirstOrDefault(q => q.Name == configuration.Destination);
                if (destination is null)
                {
                    throw new InvalidOperationException($"Destination queue '{configuration.Destination}' not found");
                }

                var queueBinding = new RabbitMQQueueBinding();
                configuration.Topology = this;
                queueBinding.Initialize(configuration);
                queueBinding.SetDestination(destination);
                destination.AddBinding(queueBinding);

                binding = queueBinding;
            }
            else if (configuration.DestinationKind == RabbitMQDestinationKind.Exchange)
            {
                var destination = _exchanges.FirstOrDefault(e => e.Name == configuration.Destination);
                if (destination is null)
                {
                    throw new InvalidOperationException(
                        $"Destination exchange '{configuration.Destination}' not found");
                }

                var exchangeBinding = new RabbitMQExchangeBinding();
                configuration.Topology = this;
                exchangeBinding.Initialize(configuration);
                exchangeBinding.SetDestination(destination);
                destination.AddBinding(exchangeBinding);

                binding = exchangeBinding;
            }
            else
            {
                throw new InvalidOperationException($"Unknown destination kind: {configuration.DestinationKind}");
            }

            binding.SetSource(source);
            source.AddBinding(binding);

            _bindings.Add(binding);

            binding.Complete();

            return binding;
        }
    }

    private static bool MatchesDestination(RabbitMQBinding binding, RabbitMQBindingConfiguration configuration)
    {
        return configuration.DestinationKind switch
        {
            RabbitMQDestinationKind.Queue =>
                binding is RabbitMQQueueBinding qb && qb.Destination.Name == configuration.Destination,
            RabbitMQDestinationKind.Exchange =>
                binding is RabbitMQExchangeBinding eb && eb.Destination.Name == configuration.Destination,
            _ => false
        };
    }
}
