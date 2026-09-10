using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

/// <summary>
/// Encodes and decodes the Azure Event Hubs resume cursor. The cursor is a
/// composite that records the next sequence number to read for every observed
/// (hub, partition) pair, so a multi-hub and multi-partition subscription can
/// resume without losing events.
/// </summary>
/// <remarks>
/// Partition ids are opaque UTF-8 strings. They are not parsed as numbers and are
/// not assumed to be dense. The stored sequence number is the next sequence number
/// to read for that partition.
/// </remarks>
internal static class AzureEventHubsCompositeCursorFormatter
{
    private const byte Version = 0x01;
    private const int HeaderLength = 5;
    private const int SeqWidth = 8;
    private const int MinPartitionEntry = 13;
    private const int MinHubEntry = 22;

    private static readonly Encoding s_strictUtf8 =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    /// <summary>
    /// Computes the formatted byte length of the supplied composite cursor map before base64
    /// encoding.
    /// </summary>
    public static int GetFormattedLength(Dictionary<HubPartition, long> map)
    {
        var groups = CreateHubGroups(map);
        var length = HeaderLength;

        foreach (var group in groups)
        {
            length += 4 + s_strictUtf8.GetByteCount(group.Hub) + 4;

            foreach (var (partitionId, _) in group.Partitions)
            {
                length += 4 + s_strictUtf8.GetByteCount(partitionId) + SeqWidth;
            }
        }

        return length;
    }

    /// <summary>
    /// Formats the supplied composite cursor map into <paramref name="destination"/>, which
    /// must be at least <see cref="GetFormattedLength"/> bytes long.
    /// </summary>
    public static void Format(
        Dictionary<HubPartition, long> map,
        Span<byte> destination)
    {
        var groups = CreateHubGroups(map);

        destination[0] = Version;
        BinaryPrimitives.WriteInt32BigEndian(destination[1..], groups.Count);

        var position = HeaderLength;

        foreach (var group in groups)
        {
            var hubLength = s_strictUtf8.GetBytes(
                group.Hub.AsSpan(),
                destination[(position + 4)..]);

            BinaryPrimitives.WriteInt32BigEndian(destination[position..], hubLength);
            position += 4 + hubLength;

            BinaryPrimitives.WriteInt32BigEndian(destination[position..], group.Partitions.Count);
            position += 4;

            foreach (var (partitionId, nextSequenceNumber) in group.Partitions)
            {
                var partitionIdLength = s_strictUtf8.GetBytes(
                    partitionId.AsSpan(),
                    destination[(position + 4)..]);

                BinaryPrimitives.WriteInt32BigEndian(destination[position..], partitionIdLength);
                position += 4 + partitionIdLength;

                BinaryPrimitives.WriteInt64BigEndian(destination[position..], nextSequenceNumber);
                position += SeqWidth;
            }
        }
    }

    /// <summary>
    /// Decodes a base64 composite cursor into a resume state. The cursor hub set must exactly
    /// match the subscription's current <paramref name="topics"/>.
    /// </summary>
    public static AzureEventHubsResumeState Parse(string cursor, string[] topics)
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

    internal static AzureEventHubsResumeState ParseDecoded(
        ReadOnlySpan<byte> cursor,
        string[] topics)
    {
        if (cursor.Length < HeaderLength || cursor[0] != Version)
        {
            throw new InvalidEventMessageCursorException();
        }

        var hubCount = BinaryPrimitives.ReadInt32BigEndian(cursor[1..]);

        if (hubCount < 0 || hubCount > (cursor.Length - HeaderLength) / MinHubEntry)
        {
            throw new InvalidEventMessageCursorException();
        }

        var nextSequenceNumbers = new Dictionary<HubPartition, long>();
        var mintedPartitionIds = new Dictionary<string, IReadOnlySet<string>>(hubCount);
        var seenHubs = new HashSet<string>(StringComparer.Ordinal);
        var position = HeaderLength;

        for (var i = 0; i < hubCount; i++)
        {
            if (cursor.Length - position < 4)
            {
                throw new InvalidEventMessageCursorException();
            }

            var hubNameLength = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
            position += 4;

            if (hubNameLength <= 0 || hubNameLength > cursor.Length - position - 4 - MinPartitionEntry)
            {
                throw new InvalidEventMessageCursorException();
            }

            var hubNameBytes = cursor.Slice(position, hubNameLength);

            if (!TryGetHub(hubNameBytes, topics, out var hub))
            {
                throw new InvalidEventMessageCursorException();
            }

            position += hubNameLength;

            if (seenHubs.Contains(hub))
            {
                throw new InvalidEventMessageCursorException();
            }

            if (cursor.Length - position < 4)
            {
                throw new InvalidEventMessageCursorException();
            }

            var partitionCount = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
            position += 4;

            if (partitionCount <= 0 || partitionCount > (cursor.Length - position) / MinPartitionEntry)
            {
                throw new InvalidEventMessageCursorException();
            }

            var seenPartitionIds = new HashSet<string>(StringComparer.Ordinal);

            for (var partition = 0; partition < partitionCount; partition++)
            {
                if (cursor.Length - position < 4)
                {
                    throw new InvalidEventMessageCursorException();
                }

                var partitionIdLength = BinaryPrimitives.ReadInt32BigEndian(cursor[position..]);
                position += 4;

                if (partitionIdLength <= 0 || partitionIdLength > cursor.Length - position - SeqWidth)
                {
                    throw new InvalidEventMessageCursorException();
                }

                var partitionIdBytes = cursor.Slice(position, partitionIdLength);
                string partitionId;

                try
                {
                    partitionId = s_strictUtf8.GetString(partitionIdBytes);
                }
                catch (DecoderFallbackException)
                {
                    throw new InvalidEventMessageCursorException();
                }

                position += partitionIdLength;

                if (!seenPartitionIds.Add(partitionId))
                {
                    throw new InvalidEventMessageCursorException();
                }

                if (cursor.Length - position < SeqWidth)
                {
                    throw new InvalidEventMessageCursorException();
                }

                var nextSequenceNumber = BinaryPrimitives.ReadInt64BigEndian(cursor[position..]);
                position += SeqWidth;

                if (nextSequenceNumber < 0 || nextSequenceNumber == long.MaxValue)
                {
                    throw new InvalidEventMessageCursorException();
                }

                nextSequenceNumbers[new HubPartition(hub, partitionId)] = nextSequenceNumber;
            }

            mintedPartitionIds[hub] = seenPartitionIds;
            seenHubs.Add(hub);
        }

        if (position != cursor.Length)
        {
            throw new InvalidEventMessageCursorException();
        }

        if (seenHubs.Count != topics.Length)
        {
            throw new InvalidEventMessageCursorException();
        }

        return new AzureEventHubsResumeState
        {
            NextSequenceNumbers = nextSequenceNumbers,
            MintedPartitionIds = mintedPartitionIds
        };
    }

    private static List<HubGroup> CreateHubGroups(Dictionary<HubPartition, long> map)
    {
        var groupMap = new Dictionary<string, HubGroup>(StringComparer.Ordinal);

        foreach (var (hubPartition, nextSequenceNumber) in map)
        {
            if (!groupMap.TryGetValue(hubPartition.Hub, out var group))
            {
                group = new HubGroup(hubPartition.Hub);
                groupMap.Add(hubPartition.Hub, group);
            }

            group.Partitions.Add((hubPartition.PartitionId, nextSequenceNumber));
        }

        var groups = new List<HubGroup>(groupMap.Values);
        groups.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Hub, right.Hub));

        foreach (var group in groups)
        {
            group.Partitions.Sort(static (left, right) =>
                StringComparer.Ordinal.Compare(left.PartitionId, right.PartitionId));
        }

        return groups;
    }

    private static bool TryGetHub(
        ReadOnlySpan<byte> cursorHub,
        string[] topics,
        [NotNullWhen(true)] out string? hub)
    {
        string cursorHubText;

        try
        {
            cursorHubText = s_strictUtf8.GetString(cursorHub);
        }
        catch (DecoderFallbackException)
        {
            hub = null;
            return false;
        }

        for (var i = 0; i < topics.Length; i++)
        {
            var candidate = topics[i];

            if (string.Equals(cursorHubText, candidate, StringComparison.Ordinal))
            {
                hub = candidate;
                return true;
            }
        }

        hub = null;
        return false;
    }

    private static int GetMaxBase64DecodedLength(int length)
        => (length + 3) / 4 * 3;

    private sealed class HubGroup(string hub)
    {
        public string Hub { get; } = hub;

        public List<(string PartitionId, long NextSequenceNumber)> Partitions { get; } = [];
    }
}
