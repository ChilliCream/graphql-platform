using System.Buffers.Binary;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

/// <summary>
/// Reuses the invariant formatted prefix of a single (topic, partition) cursor across events so
/// that only the changing offset is re-encoded. A subscription that observes exactly one partition
/// keeps the same prefix for its whole lifetime.
/// </summary>
internal sealed class SingleEntryCursorCache
{
    private string? _topic;
    private int _partition;
    private byte[]? _prefix;

    /// <summary>
    /// Gets the formatted (pre base64) byte length for the supplied entry.
    /// </summary>
    public int GetFormattedLength(TopicPartition topicPartition)
    {
        EnsurePrefix(topicPartition);
        return _prefix!.Length + sizeof(long);
    }

    /// <summary>
    /// Formats the single entry at <paramref name="offset"/> into <paramref name="destination"/>,
    /// reusing the cached invariant prefix. The output is byte-identical to the general codec for a
    /// map that holds only this entry.
    /// </summary>
    public void Format(TopicPartition topicPartition, long offset, Span<byte> destination)
    {
        EnsurePrefix(topicPartition);
        var prefix = _prefix!;
        prefix.CopyTo(destination);
        BinaryPrimitives.WriteInt64BigEndian(destination[prefix.Length..], offset);
    }

    private void EnsurePrefix(TopicPartition topicPartition)
    {
        if (_prefix is not null
            && _partition == topicPartition.Partition.Value
            && string.Equals(_topic, topicPartition.Topic, StringComparison.Ordinal))
        {
            return;
        }

        var topic = topicPartition.Topic;
        var prefix = new byte[KafkaCompositeCursorFormatter.GetSinglePrefixLength(topic)];
        KafkaCompositeCursorFormatter.WriteSinglePrefix(topic, topicPartition.Partition, prefix);

        _topic = topic;
        _partition = topicPartition.Partition.Value;
        _prefix = prefix;
    }
}
