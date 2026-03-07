namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages the RabbitMQ topology model (exchanges, queues, and bindings) for a transport instance,
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
public sealed class RabbitMQMessagingTopology(RabbitMQMessagingTransport transport, Uri baseAddress)
    : MessagingTopology<RabbitMQMessagingTransport>(transport, baseAddress)
{
    private readonly object _lock = new();
    private readonly List<RabbitMQExchange> _exchanges = [];
    private readonly List<RabbitMQQueue> _queues = [];
    private readonly List<RabbitMQBinding> _bindings = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision { get; internal set; } = true;

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
    /// Adds a new exchange to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The exchange configuration specifying name, type, durability, and arguments.</param>
    /// <returns>The created and initialized exchange resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an exchange with the same name already exists.</exception>
    public RabbitMQExchange AddExchange(RabbitMQExchangeConfiguration configuration)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == configuration.Name);
            if (exchange is not null)
            {
                throw new InvalidOperationException($"Exchange '{configuration.Name}' already exists");
            }

            exchange = new RabbitMQExchange();

            configuration.Topology = this;
            exchange.Initialize(configuration);

            _exchanges.Add(exchange);

            exchange.Complete();

            return exchange;
        }
    }

    /// <summary>
    /// Adds a new queue to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The queue configuration specifying name, durability, exclusivity, and arguments.</param>
    /// <returns>The created and initialized queue resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a queue with the same name already exists.</exception>
    public RabbitMQQueue AddQueue(RabbitMQQueueConfiguration configuration)
    {
        lock (_lock)
        {
            configuration.Topology ??= this;

            var queue = _queues.FirstOrDefault(q => q.Name == configuration.Name);
            if (queue is not null)
            {
                throw new InvalidOperationException($"Queue '{configuration.Name}' already exists");
            }

            configuration.Topology = this;
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
}
