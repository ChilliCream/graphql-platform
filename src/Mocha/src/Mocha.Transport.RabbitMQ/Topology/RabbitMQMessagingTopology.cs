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
    /// Gets the exchange with the same name, or creates it from the provided configuration.
    /// Existing exchanges are returned unchanged.
    /// </summary>
    public RabbitMQExchange GetOrAddExchange(
        string name,
        Func<string, RabbitMQExchangeConfiguration> factory)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == name);
            if (exchange is not null)
            {
                return exchange;
            }

            var configuration = factory(name);
            configuration.Name = name;
            return CreateExchange(configuration);
        }
    }

    /// <summary>
    /// Adds an exchange to the topology, or returns the existing exchange with the same name.
    /// </summary>
    public RabbitMQExchange AddExchange(RabbitMQExchangeConfiguration configuration)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == configuration.Name);
            if (exchange is not null)
            {
                return exchange;
            }

            return CreateExchange(configuration);
        }
    }

    private RabbitMQExchange CreateExchange(RabbitMQExchangeConfiguration configuration)
    {
        var exchange = new RabbitMQExchange();

        configuration.Topology = this;
        defaults.Exchange.ApplyTo(configuration);
        exchange.Initialize(configuration);

        _exchanges.Add(exchange);

        exchange.Complete();

        return exchange;
    }

    /// <summary>
    /// Gets the queue with the same name, or creates it from the provided configuration.
    /// Existing queues are returned unchanged.
    /// </summary>
    public RabbitMQQueue GetOrAddQueue(
        string name,
        Func<string, RabbitMQQueueConfiguration> factory)
    {
        lock (_lock)
        {
            var queue = _queues.FirstOrDefault(q => q.Name == name);
            if (queue is not null)
            {
                return queue;
            }

            var configuration = factory(name);
            configuration.Name = name;
            return CreateQueue(configuration);
        }
    }

    /// <summary>
    /// Adds a queue to the topology, or returns the existing queue with the same name.
    /// </summary>
    public RabbitMQQueue AddQueue(RabbitMQQueueConfiguration configuration)
    {
        lock (_lock)
        {
            var queue = _queues.FirstOrDefault(q => q.Name == configuration.Name);
            if (queue is not null)
            {
                return queue;
            }

            return CreateQueue(configuration);
        }
    }

    private RabbitMQQueue CreateQueue(RabbitMQQueueConfiguration configuration)
    {
        configuration.Topology = this;

        defaults.Queue.ApplyTo(configuration);

        var queue = new RabbitMQQueue();
        queue.Initialize(configuration);

        _queues.Add(queue);

        queue.Complete();

        return queue;
    }

    /// <summary>
    /// Gets the binding with the same identity, or creates it from the provided configuration.
    /// Existing bindings are returned unchanged.
    /// </summary>
    public RabbitMQBinding GetOrAddBinding(
        string source,
        string destination,
        RabbitMQDestinationKind destinationKind,
        Func<string, string, RabbitMQDestinationKind, RabbitMQBindingConfiguration> factory)
    {
        lock (_lock)
        {
            var existing = FindBinding(source, destination, destinationKind);
            if (existing is not null)
            {
                return existing;
            }

            var configuration = factory(source, destination, destinationKind);
            configuration.Source = source;
            configuration.Destination = destination;
            configuration.DestinationKind = destinationKind;
            return CreateBinding(configuration);
        }
    }

    /// <summary>
    /// Adds a binding to the topology or updates the existing binding with the same identity.
    /// </summary>
    public RabbitMQBinding AddBinding(RabbitMQBindingConfiguration configuration)
    {
        lock (_lock)
        {
            var existing = FindBinding(configuration);
            if (existing is not null)
            {
                MergeBindingConfiguration(existing, configuration);
                return existing;
            }

            return CreateBinding(configuration);
        }
    }

    private RabbitMQBinding? FindBinding(RabbitMQBindingConfiguration configuration)
        => FindBinding(
            configuration.Source,
            configuration.Destination,
            configuration.DestinationKind,
            configuration.RoutingKey,
            configuration.Arguments?.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)));

    private RabbitMQBinding? FindBinding(
        string source,
        string destination,
        RabbitMQDestinationKind destinationKind,
        string? routingKey = null,
        IEnumerable<KeyValuePair<string, object?>>? arguments = null)
    {
        var normalizedRoutingKey = routingKey ?? string.Empty;

        return _bindings.FirstOrDefault(b =>
            b.Source.Name == source
            && b.RoutingKey == normalizedRoutingKey
            && RabbitMQBinding.ArgumentsEqual(b.Arguments, arguments)
            && MatchesDestination(b, destination, destinationKind));
    }

    private RabbitMQBinding CreateBinding(RabbitMQBindingConfiguration configuration)
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

    private static void MergeBindingConfiguration(
        RabbitMQBinding binding,
        RabbitMQBindingConfiguration configuration)
    {
        StrengthenAutoProvision(
            binding.AutoProvision,
            configuration.AutoProvision,
            value => binding.AutoProvision = value);
    }

    private static void StrengthenAutoProvision(
        bool? existing,
        bool? incoming,
        Action<bool?> assign)
    {
        if (existing is null)
        {
            assign(incoming);
        }
        else if (incoming == true)
        {
            assign(true);
        }
    }

    private static bool MatchesDestination(RabbitMQBinding binding, RabbitMQBindingConfiguration configuration)
        => MatchesDestination(binding, configuration.Destination, configuration.DestinationKind);

    private static bool MatchesDestination(
        RabbitMQBinding binding,
        string destination,
        RabbitMQDestinationKind destinationKind)
    {
        return destinationKind switch
        {
            RabbitMQDestinationKind.Queue =>
                binding is RabbitMQQueueBinding qb && qb.Destination.Name == destination,
            RabbitMQDestinationKind.Exchange =>
                binding is RabbitMQExchangeBinding eb && eb.Destination.Name == destination,
            _ => false
        };
    }
}
