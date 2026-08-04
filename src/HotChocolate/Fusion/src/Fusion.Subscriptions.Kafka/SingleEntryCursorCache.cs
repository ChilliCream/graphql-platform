using System.Buffers.Binary;
using System.Diagnostics;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

/// <summary>
/// Reuses the invariant formatted prefix of a subscription that observes exactly one
/// (topic, partition 0) so that only the changing offset is re-encoded.
/// </summary>
internal sealed class SingleEntryCursorCache
{
    private string? _topic;
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
        Debug.Assert(topicPartition.Partition.Value == 0);

        if (_prefix is not null
            && string.Equals(_topic, topicPartition.Topic, StringComparison.Ordinal))
        {
            return;
        }

        var topic = topicPartition.Topic;
        var prefix = new byte[KafkaCompositeCursorFormatter.GetSinglePrefixLength(topic)];
        KafkaCompositeCursorFormatter.WriteSinglePrefix(topic, prefix);

        _topic = topic;
        _prefix = prefix;
    }
}
