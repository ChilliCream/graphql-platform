using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

/// <summary>
/// The decoded resume cursor: the next offset to read for every observed (topic, partition)
/// pair, plus the number of partitions each topic had when the cursor was minted. The offsets
/// are dense per topic (partition 0 through N-1 are all present), and the minted counts are used
/// to detect a partition-count shrink when a subscription resumes.
/// </summary>
internal sealed class KafkaResumeState
{
    public required Dictionary<TopicPartition, long> Offsets { get; init; }

    public required Dictionary<string, int> MintedPartitionCounts { get; init; }
}
