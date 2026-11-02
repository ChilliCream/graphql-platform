namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Manages the Azure Service Bus messaging topology including topics, queues, and subscriptions.
/// Thread-safe for concurrent access during endpoint discovery and provisioning.
/// </summary>
public sealed class AzureServiceBusMessagingTopology(
    AzureServiceBusMessagingTransport transport,
    Uri address,
    AzureServiceBusBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<AzureServiceBusMessagingTransport>(transport, address)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<AzureServiceBusTopic> _topics = [];
    private readonly List<AzureServiceBusQueue> _queues = [];
    private readonly List<AzureServiceBusSubscription> _subscriptions = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets all topics in this topology.
    /// </summary>
    public IReadOnlyList<AzureServiceBusTopic> Topics => _topics;

    /// <summary>
    /// Gets all queues in this topology.
    /// </summary>
    public IReadOnlyList<AzureServiceBusQueue> Queues => _queues;

    /// <summary>
    /// Gets all subscriptions in this topology.
    /// </summary>
    public IReadOnlyList<AzureServiceBusSubscription> Subscriptions => _subscriptions;

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned queues and topics.
    /// </summary>
    public AzureServiceBusBusDefaults Defaults => defaults;

    /// <summary>
    /// Gets the topic with the same name, or creates it from the provided configuration.
    /// Existing topics are returned unchanged.
    /// </summary>
    public AzureServiceBusTopic GetOrAddTopic(
        string name,
        Func<string, AzureServiceBusTopicConfiguration> factory)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(t => t.Name == name);
            if (topic is not null)
            {
                return topic;
            }

            var configuration = factory(name);
            configuration.Name = name;
            return CreateTopic(configuration);
        }
    }

    /// <summary>
    /// Adds a topic to the topology or returns the existing topic with the same name.
    /// </summary>
    public AzureServiceBusTopic AddTopic(AzureServiceBusTopicConfiguration configuration)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(t => t.Name == configuration.Name);
            if (topic is not null)
            {
                return topic;
            }

            return CreateTopic(configuration);
        }
    }

    private AzureServiceBusTopic CreateTopic(AzureServiceBusTopicConfiguration configuration)
    {
        var topic = new AzureServiceBusTopic();

        configuration.Topology = this;
        defaults.Topic.ApplyTo(configuration);
        topic.Initialize(configuration);

        _topics.Add(topic);

        topic.Complete();

        return topic;
    }

    /// <summary>
    /// Gets the queue with the same name, or creates it from the provided configuration.
    /// Existing queues are returned unchanged.
    /// </summary>
    public AzureServiceBusQueue GetOrAddQueue(
        string name,
        Func<string, AzureServiceBusQueueConfiguration> factory)
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
    /// Adds a queue to the topology or returns the existing queue with the same name.
    /// </summary>
    public AzureServiceBusQueue AddQueue(AzureServiceBusQueueConfiguration configuration)
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

    private AzureServiceBusQueue CreateQueue(AzureServiceBusQueueConfiguration configuration)
    {
        configuration.Topology = this;
        defaults.Queue.ApplyTo(configuration);

        var queue = new AzureServiceBusQueue();
        queue.Initialize(configuration);

        _queues.Add(queue);

        queue.Complete();

        return queue;
    }

    /// <summary>
    /// Gets the subscription with the same source and destination, or creates it from the provided configuration.
    /// Existing subscriptions are returned unchanged.
    /// </summary>
    public AzureServiceBusSubscription EnsureSubscription(
        string source,
        string destination,
        Func<string, string, AzureServiceBusSubscriptionConfiguration> factory)
    {
        lock (_lock)
        {
            var subscription = _subscriptions.FirstOrDefault(s =>
                s.Source.Name == source && s.Destination.Name == destination);
            if (subscription is not null)
            {
                return subscription;
            }

            var configuration = factory(source, destination);
            configuration.Source = source;
            configuration.Destination = destination;
            return CreateSubscription(configuration);
        }
    }

    /// <summary>
    /// Adds a subscription to the topology, or returns the existing subscription with the same source and destination.
    /// </summary>
    /// <param name="configuration">The subscription configuration specifying source topic and destination queue.</param>
    /// <returns>The created or existing subscription resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source topic or destination queue is not found in the topology.</exception>
    public AzureServiceBusSubscription AddSubscription(AzureServiceBusSubscriptionConfiguration configuration)
    {
        lock (_lock)
        {
            var subscription = _subscriptions.FirstOrDefault(s =>
                s.Source.Name == configuration.Source && s.Destination.Name == configuration.Destination);
            if (subscription is not null)
            {
                return subscription;
            }

            return CreateSubscription(configuration);
        }
    }

    private AzureServiceBusSubscription CreateSubscription(
        AzureServiceBusSubscriptionConfiguration configuration)
    {
        var sourceName = configuration.Source
            ?? throw new InvalidOperationException("Subscription source topic is required.");
        var destinationName = configuration.Destination
            ?? throw new InvalidOperationException("Subscription destination queue is required.");

        var source = _topics.FirstOrDefault(t => t.Name == sourceName) ??
            throw ThrowHelper.TopologyTopicNotFound(sourceName);

        var destination = _queues.FirstOrDefault(q => q.Name == destinationName) ??
            throw ThrowHelper.TopologyQueueNotFound(destinationName);

        if (configuration.RequiresSession == true)
        {
            throw new InvalidOperationException(
                $"Azure Service Bus subscription from '{sourceName}' to '{destinationName}' "
                + "cannot require sessions because modeled subscriptions auto-forward messages.");
        }

        configuration.Topology = this;

        var subscription = new AzureServiceBusSubscription();
        subscription.Initialize(configuration);
        subscription.SetSource(source);
        subscription.SetDestination(destination);

        source.AddSubscription(subscription);
        destination.AddSubscription(subscription);
        _subscriptions.Add(subscription);

        subscription.Complete();

        return subscription;
    }
}
