using System.Buffers;
using System.Buffers.Binary;
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
/// [version : 1 byte  = 0x01]
/// [count   : 4 bytes, big endian int32]
/// repeated count times:
///   [topicByteLength : 4 bytes, big endian int32]
///   [topic           : topicByteLength bytes, UTF-8]
///   [partition       : 4 bytes, big endian int32]
///   [offset          : 8 bytes, big endian int64]
/// </code>
/// The leading version byte makes the format unambiguous and lets future revisions be detected.
/// The map (and therefore the encoded cursor) grows with the number of distinct
/// (topic, partition) pairs the subscription has delivered from.
/// </remarks>
internal static class KafkaCompositeCursorFormatter
{
    private const byte Version = 1;
    private const int HeaderLength = 5;
    private const int EntryFixedLength = 4 + 4 + 8;

    private static readonly Encoding s_strictUtf8 =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    /// <summary>
    /// Computes the formatted (pre base64) byte length of the supplied cursor map.
    /// </summary>
    public static int GetFormattedLength(Dictionary<TopicPartition, long> map)
    {
        var length = HeaderLength;

        foreach (var entry in map)
        {
            length += EntryFixedLength + Encoding.UTF8.GetByteCount(entry.Key.Topic);
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
        destination[0] = Version;
        BinaryPrimitives.WriteInt32BigEndian(destination[1..], map.Count);

        var position = HeaderLength;

        foreach (var entry in map)
        {
            var topicLength = Encoding.UTF8.GetBytes(
                entry.Key.Topic.AsSpan(),
                destination[(position + 4)..]);

            BinaryPrimitives.WriteInt32BigEndian(destination[position..], topicLength);
            position += 4 + topicLength;

            BinaryPrimitives.WriteInt32BigEndian(destination[position..], entry.Key.Partition.Value);
            position += 4;

            BinaryPrimitives.WriteInt64BigEndian(destination[position..], entry.Value);
            position += 8;
        }
    }

    /// <summary>
    /// Computes the formatted (pre base64) byte length of a single (topic, partition) cursor.
    /// </summary>
    public static int GetSingleFormattedLength(string topic)
        => HeaderLength + EntryFixedLength + Encoding.UTF8.GetByteCount(topic);

    /// <summary>
    /// Formats a single (topic, partition) cursor at <paramref name="offset"/> into
    /// <paramref name="destination"/>. The output is byte-identical to <see cref="Format"/> for a
    /// map that holds only this entry, so both encodings share one wire format and a resume cursor
    /// interoperates between them.
    /// </summary>
    public static void FormatSingle(
        string topic,
        Partition partition,
        long offset,
        Span<byte> destination)
    {
        var prefixLength = WriteSinglePrefix(topic, partition, destination);
        BinaryPrimitives.WriteInt64BigEndian(destination[prefixLength..], offset);
    }

    // Writes the invariant single-entry prefix (version, count = 1, topic length, topic, partition)
    // and returns its length. The trailing 8-byte offset is written by the caller so that the prefix
    // can be cached and reused across events that differ only by their offset.
    internal static int WriteSinglePrefix(string topic, Partition partition, Span<byte> destination)
    {
        destination[0] = Version;
        BinaryPrimitives.WriteInt32BigEndian(destination[1..], 1);

        var topicLength = Encoding.UTF8.GetBytes(topic.AsSpan(), destination[(HeaderLength + 4)..]);
        BinaryPrimitives.WriteInt32BigEndian(destination[HeaderLength..], topicLength);

        var position = HeaderLength + 4 + topicLength;
        BinaryPrimitives.WriteInt32BigEndian(destination[position..], partition.Value);

        return position + 4;
    }

    // The invariant single-entry prefix length (everything except the trailing 8-byte offset).
    internal static int GetSinglePrefixLength(string topic)
        => HeaderLength + 4 + Encoding.UTF8.GetByteCount(topic) + 4;

    /// <summary>
    /// Decodes a base64 cursor into a mutable map. Every topic in the cursor must be one of the
    /// subscription's current <paramref name="topics"/>; a malformed cursor or a cross-subscription
    /// topic results in an <see cref="InvalidEventMessageCursorException"/>.
    /// </summary>
    public static Dictionary<TopicPartition, long> Parse(string cursor, string[] topics)
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

    private static Dictionary<TopicPartition, long> ParseDecoded(
        ReadOnlySpan<byte> cursor,
        string[] topics)
    {
        if (cursor.Length < HeaderLength || cursor[0] != Version)
        {
            throw new InvalidEventMessageCursorException();
        }

        var count = BinaryPrimitives.ReadInt32BigEndian(cursor[1..]);

        // The count is read from an untrusted cursor, so it must be validated against the buffer
        // before it is used to size the map. The smallest possible entry is EntryFixedLength plus a
        // single topic byte, so a buffer can never hold more than that many entries. Rejecting an
        // impossible count here keeps a crafted cursor from forcing an oversized allocation.
        if (count < 0 || count > (cursor.Length - HeaderLength) / (EntryFixedLength + 1))
        {
            throw new InvalidEventMessageCursorException();
        }

        var map = new Dictionary<TopicPartition, long>(count);
        var position = HeaderLength;

        for (var i = 0; i < count; i++)
        {
            if (cursor.Length - position < 4)
            {
                throw new InvalidEventMessageCursorException();
            }

            var topicLength = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
            position += 4;

            // The remaining bytes must cover the topic plus the fixed partition and offset fields.
            // The comparison keeps the untrusted topicLength on the left and only small, in-bounds
            // terms on the right, so a near int.MaxValue topicLength cannot overflow past the guard
            // and surface as an ArgumentOutOfRangeException from the slice below.
            if (topicLength <= 0 || topicLength > cursor.Length - position - (EntryFixedLength - 4))
            {
                throw new InvalidEventMessageCursorException();
            }

            var topicBytes = cursor.Slice(position, topicLength);
            position += topicLength;

            if (!TryGetTopic(topicBytes, topics, out var topic))
            {
                throw new InvalidEventMessageCursorException();
            }

            var partition = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
            position += 4;

            if (partition < 0)
            {
                throw new InvalidEventMessageCursorException();
            }

            var offset = BinaryPrimitives.ReadInt64BigEndian(cursor[position..]);
            position += 8;

            if (offset < 0 || offset == long.MaxValue)
            {
                throw new InvalidEventMessageCursorException();
            }

            if (!map.TryAdd(new TopicPartition(topic, new Partition(partition)), offset))
            {
                throw new InvalidEventMessageCursorException();
            }
        }

        if (position != cursor.Length)
        {
            throw new InvalidEventMessageCursorException();
        }

        return map;
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
