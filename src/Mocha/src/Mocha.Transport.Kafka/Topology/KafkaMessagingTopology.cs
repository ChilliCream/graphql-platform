namespace Mocha.Transport.Kafka;

/// <summary>
/// Manages the Kafka topology model (topics and consumer groups) for a transport instance,
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
public sealed class KafkaMessagingTopology(
    KafkaMessagingTransport transport,
    Uri baseAddress,
    KafkaBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<KafkaMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<KafkaTopic> _topics = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets the list of topics registered in this topology.
    /// </summary>
    public IReadOnlyList<KafkaTopic> Topics => _topics;

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned topics.
    /// </summary>
    public KafkaBusDefaults Defaults => defaults;

    /// <summary>
    /// Adds a new topic to the topology, initializing it from the given configuration.
    /// </summary>
    /// <param name="configuration">The topic configuration specifying name, partitions, replication, and settings.</param>
    /// <returns>The created and initialized topic resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a topic with the same name already exists.</exception>
    public KafkaTopic AddTopic(KafkaTopicConfiguration configuration)
    {
        lock (_lock)
        {
            var topic = _topics.FirstOrDefault(t => t.Name == configuration.Name);
            if (topic is not null)
            {
                throw new InvalidOperationException($"Topic '{configuration.Name}' already exists");
            }

            topic = new KafkaTopic();
            configuration.Topology = this;
            defaults.Topic.ApplyTo(configuration);
            topic.Initialize(configuration);
            _topics.Add(topic);
            topic.Complete();
            return topic;
        }
    }
}
