namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Manages the Event Hub topology model (topics and subscriptions) for a transport instance
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
public sealed class EventHubMessagingTopology(
    EventHubMessagingTransport transport,
    Uri baseAddress,
    EventHubBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<EventHubMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<EventHubTopic> _topics = [];
    private readonly List<EventHubSubscription> _subscriptions = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets the list of topics (Event Hub entities) registered in this topology.
    /// </summary>
    public IReadOnlyList<EventHubTopic> Topics => _topics;

    /// <summary>
    /// Gets the list of subscriptions (consumer groups) registered in this topology.
    /// </summary>
    public IReadOnlyList<EventHubSubscription> Subscriptions => _subscriptions;

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned topics and subscriptions.
    /// </summary>
    public EventHubBusDefaults Defaults => defaults;

    /// <summary>
    /// Adds a new topic to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The topic configuration specifying name, partition count, and provisioning.</param>
    /// <returns>The created and initialized topic resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a topic with the same name already exists.</exception>
    public EventHubTopic AddTopic(EventHubTopicConfiguration configuration)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(t => t.Name == configuration.Name);
            if (topic is not null)
            {
                throw new InvalidOperationException($"Topic '{configuration.Name}' already exists");
            }

            topic = new EventHubTopic();
            configuration.Topology = this;
            defaults.Topic.ApplyTo(configuration);
            topic.Initialize(configuration);
            _topics.Add(topic);
            topic.Complete();

            return topic;
        }
    }

    /// <summary>
    /// Adds a new subscription (consumer group) to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The subscription configuration specifying topic name, consumer group, and provisioning.</param>
    /// <returns>The created and initialized subscription resource.</returns>
    public EventHubSubscription AddSubscription(EventHubSubscriptionConfiguration configuration)
    {
        lock (_lock)
        {
            var sub = new EventHubSubscription();
            configuration.Topology = this;
            sub.Initialize(configuration);
            _subscriptions.Add(sub);
            sub.Complete();
            return sub;
        }
    }
}
