using Confluent.Kafka;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Configuration for a Kafka messaging transport, extending the base transport configuration
/// with Kafka-specific connection and producer/consumer settings.
/// </summary>
public sealed class KafkaTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name used when no explicit name is specified.
    /// </summary>
    public const string DefaultName = "kafka";

    /// <summary>
    /// The default URI schema used for Kafka transport addresses.
    /// </summary>
    public const string DefaultSchema = "kafka";

    /// <summary>
    /// Creates a new configuration instance with the default name and schema.
    /// </summary>
    public KafkaTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets the Kafka bootstrap servers connection string.
    /// </summary>
    public string? BootstrapServers { get; set; }

    /// <summary>
    /// Gets or sets an optional delegate to override producer configuration settings.
    /// </summary>
    public Action<ProducerConfig>? ProducerConfigOverrides { get; set; }

    /// <summary>
    /// Gets or sets an optional delegate to override consumer configuration settings.
    /// </summary>
    public Action<ConsumerConfig>? ConsumerConfigOverrides { get; set; }

    /// <summary>
    /// Gets or sets the explicitly declared topics for this transport.
    /// </summary>
    public List<KafkaTopicConfiguration> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether topology resources (topics)
    /// should be automatically provisioned on the broker. When <c>null</c>, defaults to <c>true</c>.
    /// Individual resources can override this setting.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets the bus-level defaults applied to all auto-provisioned topics.
    /// </summary>
    public KafkaBusDefaults Defaults { get; set; } = new();
}
