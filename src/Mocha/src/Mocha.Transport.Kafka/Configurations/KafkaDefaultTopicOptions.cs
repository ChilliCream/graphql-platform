namespace Mocha.Transport.Kafka;

/// <summary>
/// Default options for topics created by topology conventions.
/// </summary>
public sealed class KafkaDefaultTopicOptions
{
    /// <summary>
    /// Gets or sets the default number of partitions for auto-provisioned topics.
    /// </summary>
    public int? Partitions { get; set; }

    /// <summary>
    /// Gets or sets the default replication factor for auto-provisioned topics.
    /// </summary>
    public short? ReplicationFactor { get; set; }

    /// <summary>
    /// Gets or sets the default topic-level configs (e.g., retention.ms, cleanup.policy).
    /// </summary>
    public Dictionary<string, string>? TopicConfigs { get; set; }

    /// <summary>
    /// Applies these defaults to a topic configuration, filling in
    /// any values that are not explicitly set.
    /// </summary>
    internal void ApplyTo(KafkaTopicConfiguration configuration)
    {
        configuration.Partitions ??= Partitions;
        configuration.ReplicationFactor ??= ReplicationFactor;

        if (TopicConfigs is not null && configuration.TopicConfigs is null)
        {
            configuration.TopicConfigs = new Dictionary<string, string>(TopicConfigs);
        }
    }
}
