using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

/// <summary>
/// Encodes and decodes the Kafka resume cursor. The cursor is a composite that records the next
/// offset to read for every (topic, partition) pair a subscription has observed, so that a
/// multi-topic and multi-partition subscription can resume all of them without losing events.
/// </summary>
/// <remarks>
/// The wire layout is a versioned, self describing binary blob (later base64 encoded):
/// <code>
/// [version    : 1 byte  = 0x02]
/// [topicCount : 4 bytes, big endian int32]
/// repeated topicCount times:
///   [topicByteLength : 4 bytes, big endian int32]
///   [topic           : topicByteLength bytes, UTF-8]
///   [partitionCount  : 4 bytes, big endian int32]
///   [offsets         : partitionCount values, each an 8-byte big endian int64]
/// </code>
/// A topic's offsets are positional: the offset at index <c>j</c> is the next offset to read for
/// partition <c>j</c>. The partition count records how many partitions existed when the cursor was
/// minted, allowing a later resume to detect when the live topic has fewer partitions.
/// </remarks>
internal static class KafkaCompositeCursorFormatter
{
    private const byte Version = 2;
    private const int HeaderLength = 5;
    private const int OffsetWidth = 8;
    private const int MinimumTopicEntryLength = 4 + 1 + 4 + OffsetWidth;

    private static readonly Encoding s_strictUtf8 =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    /// <summary>
    /// Computes the formatted (pre base64) byte length of the supplied cursor map.
    /// </summary>
    public static int GetFormattedLength(Dictionary<TopicPartition, long> map)
    {
        var groups = CreateTopicGroups(map);
        var length = HeaderLength;

        foreach (var (topic, count) in groups)
        {
            length += 4 + Encoding.UTF8.GetByteCount(topic) + 4 + count * OffsetWidth;
        }

        return length;
    }

    /// <summary>
    /// Formats the supplied cursor map into <paramref name="destination"/>, which must be at
    /// least <see cref="GetFormattedLength"/> bytes long.
    /// </summary>
    public static void Format(
        Dictionary<TopicPartition, long> map,
        Span<byte> destination)
    {
        var groups = CreateTopicGroups(map);

        destination[0] = Version;
        BinaryPrimitives.WriteInt32BigEndian(destination[1..], groups.Count);

        var position = HeaderLength;

        foreach (var (topic, count) in groups)
        {
            var topicLength = Encoding.UTF8.GetBytes(
                topic.AsSpan(),
                destination[(position + 4)..]);

            BinaryPrimitives.WriteInt32BigEndian(destination[position..], topicLength);
            position += 4 + topicLength;

            BinaryPrimitives.WriteInt32BigEndian(destination[position..], count);
            position += 4;

            for (var partition = 0; partition < count; partition++)
            {
                var topicPartition = new TopicPartition(topic, new Partition(partition));
                var found = map.TryGetValue(topicPartition, out var offset);
                Debug.Assert(found, "Kafka resume cursor maps must be dense per topic.");

                BinaryPrimitives.WriteInt64BigEndian(destination[position..], offset);
                position += OffsetWidth;
            }
        }
    }

    /// <summary>
    /// Computes the formatted (pre base64) byte length of a single partition 0 cursor.
    /// </summary>
    public static int GetSingleFormattedLength(string topic)
        => HeaderLength + 4 + Encoding.UTF8.GetByteCount(topic) + 4 + OffsetWidth;

    /// <summary>
    /// Formats a single partition 0 cursor at <paramref name="offset"/> into
    /// <paramref name="destination"/>. The output is byte-identical to <see cref="Format"/> for a
    /// map that holds only partition 0 for the same topic.
    /// </summary>
    public static void FormatSingle(
        string topic,
        long offset,
        Span<byte> destination)
    {
        var prefixLength = WriteSinglePrefix(topic, destination);
        BinaryPrimitives.WriteInt64BigEndian(destination[prefixLength..], offset);
    }

    // Writes the invariant single-entry prefix and returns its length. The trailing 8-byte offset
    // is written by the caller so the prefix can be cached and reused across events.
    internal static int WriteSinglePrefix(string topic, Span<byte> destination)
    {
        destination[0] = Version;
        BinaryPrimitives.WriteInt32BigEndian(destination[1..], 1);

        var topicLength = Encoding.UTF8.GetBytes(topic.AsSpan(), destination[(HeaderLength + 4)..]);
        BinaryPrimitives.WriteInt32BigEndian(destination[HeaderLength..], topicLength);

        var position = HeaderLength + 4 + topicLength;
        BinaryPrimitives.WriteInt32BigEndian(destination[position..], 1);

        return position + 4;
    }

    // The invariant single-entry prefix length (everything except the trailing 8-byte offset).
    internal static int GetSinglePrefixLength(string topic)
        => HeaderLength + 4 + Encoding.UTF8.GetByteCount(topic) + 4;

    /// <summary>
    /// Decodes a base64 cursor into a resume state. The cursor's topic set must exactly match the
    /// subscription's current <paramref name="topics"/>.
    /// </summary>
    public static KafkaResumeState Parse(string cursor, string[] topics)
    {
        var maxDecodedLength = GetMaxBase64DecodedLength(cursor.Length);
        byte[]? rented = null;
        var buffer = maxDecodedLength <= 256
            ? stackalloc byte[maxDecodedLength]
            : rented = ArrayPool<byte>.Shared.Rent(maxDecodedLength);

        try
        {
            if (!Convert.TryFromBase64Chars(cursor.AsSpan(), buffer, out var bytesWritten))
            {
                throw new InvalidEventMessageCursorException();
            }

            return ParseDecoded(buffer[..bytesWritten], topics);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    internal static KafkaResumeState ParseDecoded(
        ReadOnlySpan<byte> cursor,
        string[] topics)
    {
        if (cursor.Length < HeaderLength || cursor[0] != Version)
        {
            throw new InvalidEventMessageCursorException();
        }

        var topicCount = BinaryPrimitives.ReadInt32BigEndian(cursor[1..]);

        if (topicCount < 0 || topicCount > (cursor.Length - HeaderLength) / MinimumTopicEntryLength)
        {
            throw new InvalidEventMessageCursorException();
        }

        var offsets = new Dictionary<TopicPartition, long>();
        var mintedCounts = new Dictionary<string, int>(topicCount);
        var position = HeaderLength;

        for (var i = 0; i < topicCount; i++)
        {
            if (cursor.Length - position < 4)
            {
                throw new InvalidEventMessageCursorException();
            }

            var topicLength = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
            position += 4;

            if (topicLength <= 0 || topicLength > cursor.Length - position - 12)
            {
                throw new InvalidEventMessageCursorException();
            }

            var topicBytes = cursor.Slice(position, topicLength);
            position += topicLength;

            if (!TryGetTopic(topicBytes, topics, out var topic))
            {
                throw new InvalidEventMessageCursorException();
            }

            if (mintedCounts.ContainsKey(topic))
            {
                throw new InvalidEventMessageCursorException();
            }

            if (cursor.Length - position < 4)
            {
                throw new InvalidEventMessageCursorException();
            }

            var mintedPartitionCount = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
            position += 4;

            if (mintedPartitionCount <= 0 || mintedPartitionCount > (cursor.Length - position) / OffsetWidth)
            {
                throw new InvalidEventMessageCursorException();
            }

            for (var partition = 0; partition < mintedPartitionCount; partition++)
            {
                var offset = BinaryPrimitives.ReadInt64BigEndian(cursor[position..]);
                position += OffsetWidth;

                if (offset < 0 || offset == long.MaxValue)
                {
                    throw new InvalidEventMessageCursorException();
                }

                offsets[new TopicPartition(topic, new Partition(partition))] = offset;
            }

            mintedCounts[topic] = mintedPartitionCount;
        }

        if (position != cursor.Length)
        {
            throw new InvalidEventMessageCursorException();
        }

        if (mintedCounts.Count != topics.Length)
        {
            throw new InvalidEventMessageCursorException();
        }

        return new KafkaResumeState
        {
            Offsets = offsets,
            MintedPartitionCounts = mintedCounts
        };
    }

    private static Dictionary<string, int> CreateTopicGroups(Dictionary<TopicPartition, long> map)
    {
        var groups = new Dictionary<string, int>();

        foreach (var (topicPartition, _) in map)
        {
            var topic = topicPartition.Topic;
            groups.TryGetValue(topic, out var count);
            groups[topic] = count + 1;
        }

        return groups;
    }

    private static bool TryGetTopic(
        ReadOnlySpan<byte> cursorTopic,
        string[] topics,
        [NotNullWhen(true)] out string? topic)
    {
        if (topics.Length == 1)
        {
            topic = topics[0];
            if (TopicEquals(cursorTopic, topic))
            {
                return true;
            }

            topic = null;
            return false;
        }

        int charCount;

        try
        {
            charCount = s_strictUtf8.GetCharCount(cursorTopic);
        }
        catch (DecoderFallbackException)
        {
            topic = null;
            return false;
        }

        char[]? rented = null;
        var buffer = charCount <= 256
            ? stackalloc char[charCount]
            : rented = ArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var written = s_strictUtf8.GetChars(cursorTopic, buffer);
            var cursorTopicText = buffer[..written];

            for (var i = 0; i < topics.Length; i++)
            {
                var candidate = topics[i];

                if (cursorTopicText.SequenceEqual(candidate.AsSpan()))
                {
                    topic = candidate;
                    return true;
                }
            }

            topic = null;
            return false;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private static bool TopicEquals(ReadOnlySpan<byte> cursorTopic, string topic)
    {
        var byteCount = Encoding.UTF8.GetByteCount(topic);
        if (byteCount != cursorTopic.Length)
        {
            return false;
        }

        byte[]? rented = null;
        var buffer = byteCount <= 256
            ? stackalloc byte[byteCount]
            : rented = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            var written = Encoding.UTF8.GetBytes(topic.AsSpan(), buffer);
            return cursorTopic.SequenceEqual(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static int GetMaxBase64DecodedLength(int length)
        => (length + 3) / 4 * 3;
}
