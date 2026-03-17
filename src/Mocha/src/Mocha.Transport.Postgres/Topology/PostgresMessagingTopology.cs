namespace Mocha.Transport.Postgres;

/// <summary>
/// Manages the PostgreSQL messaging topology including topics, queues, and subscriptions.
/// Thread-safe for concurrent access during endpoint discovery and provisioning.
/// </summary>
public sealed class PostgresMessagingTopology(
    PostgresMessagingTransport transport,
    Uri address,
    PostgresBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<PostgresMessagingTransport>(transport, address)
{
    private readonly object _lock = new();
    private readonly List<PostgresTopic> _topics = [];
    private readonly List<PostgresQueue> _queues = [];
    private readonly List<PostgresSubscription> _subscriptions = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets all topics in this topology.
    /// </summary>
    public IReadOnlyList<PostgresTopic> Topics => _topics;

    /// <summary>
    /// Gets all queues in this topology.
    /// </summary>
    public IReadOnlyList<PostgresQueue> Queues => _queues;

    /// <summary>
    /// Gets all subscriptions in this topology.
    /// </summary>
    public IReadOnlyList<PostgresSubscription> Subscriptions => _subscriptions;

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned queues and topics.
    /// </summary>
    public PostgresBusDefaults Defaults => defaults;

    /// <summary>
    /// Adds a new topic to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The topic configuration specifying name and provisioning settings.</param>
    /// <returns>The created and initialized topic resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a topic with the same name already exists.</exception>
    public PostgresTopic AddTopic(PostgresTopicConfiguration configuration)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(t => t.Name == configuration.Name);
            if (topic is not null)
            {
                throw new InvalidOperationException($"Topic '{configuration.Name}' already exists");
            }

            topic = new PostgresTopic();

            configuration.Topology = this;
            defaults.Topic.ApplyTo(configuration);
            topic.Initialize(configuration);

            _topics.Add(topic);

            topic.Complete();

            return topic;
        }
    }

    /// <summary>
    /// Adds a new queue to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The queue configuration specifying name, auto-delete, and provisioning settings.</param>
    /// <returns>The created and initialized queue resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a queue with the same name already exists.</exception>
    public PostgresQueue AddQueue(PostgresQueueConfiguration configuration)
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
            defaults.Queue.ApplyTo(configuration);

            queue = new PostgresQueue();
            queue.Initialize(configuration);

            _queues.Add(queue);

            queue.Complete();

            return queue;
        }
    }

    /// <summary>
    /// Adds a new subscription to the topology, connecting a source topic to a destination queue.
    /// </summary>
    /// <param name="configuration">The subscription configuration specifying source topic and destination queue.</param>
    /// <returns>The created and initialized subscription resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source topic or destination queue is not found in the topology.</exception>
    public PostgresSubscription AddSubscription(PostgresSubscriptionConfiguration configuration)
    {
        lock (_lock)
        {
            var source = _topics.FirstOrDefault(t => t.Name == configuration.Source) ??
                throw new InvalidOperationException($"Topic '{configuration.Source}' not found in topology");

            var destination = _queues.FirstOrDefault(q => q.Name == configuration.Destination) ??
                throw new InvalidOperationException($"Queue '{configuration.Destination}' not found in topology");

            if (_subscriptions.Any(s =>
                    s.Source.Name == configuration.Source && s.Destination.Name == configuration.Destination))
            {
                throw new InvalidOperationException(
                    $"Subscription from topic '{configuration.Source}' to queue '{configuration.Destination}' already exists");
            }

            configuration.Topology = this;

            var subscription = new PostgresSubscription();
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
}
