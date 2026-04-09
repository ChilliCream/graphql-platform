namespace Mocha.Transport.Kafka;

/// <summary>
/// Fluent descriptor interface for configuring a Kafka topic.
/// </summary>
public interface IKafkaTopicDescriptor : IMessagingDescriptor<KafkaTopicConfiguration>
{
    /// <summary>
    /// Sets the number of partitions for the topic.
    /// </summary>
    /// <param name="partitions">The number of partitions.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaTopicDescriptor Partitions(int partitions);

    /// <summary>
    /// Sets the replication factor for the topic.
    /// </summary>
    /// <param name="replicationFactor">The replication factor.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaTopicDescriptor ReplicationFactor(short replicationFactor);

    /// <summary>
    /// Sets whether the topic should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">Whether to auto-provision.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaTopicDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Adds a topic-level configuration entry.
    /// </summary>
    /// <param name="key">The configuration key (e.g., "retention.ms").</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaTopicDescriptor WithConfig(string key, string value);
}
