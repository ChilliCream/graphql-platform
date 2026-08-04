using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaCompositeCursorFormatterTests
{
    [Fact]
    public void Parse_Should_RestoreState_When_MultiTopicMultiPartitionCursorRoundTrips()
    {
        // arrange
        // A cursor spanning two topics, each dense from partition 0 (v2 offsets are positional).
        var topics = new[] { "topic-a", "topic-b" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 5,
            [new TopicPartition("topic-a", new Partition(1))] = 9,
            [new TopicPartition("topic-b", new Partition(0))] = 0,
            [new TopicPartition("topic-b", new Partition(1))] = 100,
            [new TopicPartition("topic-b", new Partition(2))] = 123456789
        };

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.Offsets);
        Assert.Equal(
            new Dictionary<string, int> { ["topic-a"] = 2, ["topic-b"] = 3 },
            parsed.MintedPartitionCounts);
    }

    [Fact]
    public void Parse_Should_RestoreState_When_SingleEntryCursorRoundTrips()
    {
        // arrange
        var topics = new[] { "topic-a" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 42
        };

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.Offsets);
        Assert.Equal(
            new Dictionary<string, int> { ["topic-a"] = 1 },
            parsed.MintedPartitionCounts);
    }

    [Fact]
    public void Parse_Should_RestoreState_When_DenseMultiPartitionSingleTopicRoundTrips()
    {
        // arrange
        // One topic with three dense partitions; the minted count records the full width.
        var topics = new[] { "topic-a" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1,
            [new TopicPartition("topic-a", new Partition(1))] = 2,
            [new TopicPartition("topic-a", new Partition(2))] = 3
        };

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.Offsets);
        Assert.Equal(3, parsed.MintedPartitionCounts["topic-a"]);
    }

    [Fact]
    public void Format_Should_EmitAscendingPartitionOrder_When_MapInsertedOutOfOrder()
    {
        // arrange
        // One topic with three partitions carrying distinct offsets. The two maps hold identical
        // (topic, partition, offset) content but insert the entries in different orders, so a writer
        // that leaked dictionary iteration order would produce divergent bytes.
        var topics = new[] { "topic-a" };
        var ascending = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 10,
            [new TopicPartition("topic-a", new Partition(1))] = 20,
            [new TopicPartition("topic-a", new Partition(2))] = 30
        };
        var shuffled = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(2))] = 30,
            [new TopicPartition("topic-a", new Partition(0))] = 10,
            [new TopicPartition("topic-a", new Partition(1))] = 20
        };

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Encode(shuffled), topics);

        // assert
        // The writer pins ascending partition order, so both insertion orders serialize identically,
        // and the out-of-order map still round-trips each partition to its own offset.
        Assert.Equal(Format(ascending), Format(shuffled));
        Assert.Equal(ascending, parsed.Offsets);
    }

    [Fact]
    public void Parse_Should_Throw_When_CursorIsNotBase64()
    {
        // arrange
        var topics = new[] { "topic-a" };

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse("not-base64", topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Theory]
    [InlineData((byte)0x01)]
    [InlineData((byte)0x03)]
    public void Parse_Should_Throw_When_VersionIsUnknown(byte version)
    {
        // arrange
        // A structurally valid v2 cursor whose version byte is set to an unsupported value. The
        // removed v1 (0x01) and any future (0x03) version must both be rejected.
        var topics = new[] { "topic-a" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        };
        var bytes = Format(map);
        bytes[0] = version;
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_TopicIsNotInSubscription()
    {
        // arrange
        // The cursor references a topic the subscription is not subscribed to.
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        };
        var cursor = Encode(map);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, ["topic-b"]);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_SubscriptionTopicMissingFromCursor()
    {
        // arrange
        // The subscription covers two topics but the cursor carries only one; the cursor topic set
        // must exactly equal the subscription topics.
        var topics = new[] { "topic-a", "topic-b" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        };
        var cursor = Encode(map);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_TopicIsDuplicated()
    {
        // arrange
        // Two entries for the same topic. The duplicate must be rejected before offsets are trusted.
        var topics = new[] { "topic-a" };
        var single = Format(new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        });
        var entry = single.AsSpan(5).ToArray();
        var bytes = new byte[5 + (entry.Length * 2)];
        bytes[0] = 0x02;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(1), 2);
        entry.CopyTo(bytes.AsSpan(5));
        entry.CopyTo(bytes.AsSpan(5 + entry.Length));
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_CountExceedsBufferCapacity()
    {
        // arrange
        // A tiny cursor whose topic count claims int.MaxValue entries. The count must be rejected
        // before a dictionary is pre-sized for billions of entries.
        var topics = new[] { "topic-a" };
        Span<byte> bytes = [0x02, 0x7F, 0xFF, 0xFF, 0xFF];
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_TopicLengthOverflowsGuard()
    {
        // arrange
        // A single-entry cursor whose topic length is near int.MaxValue. The length guard must reject
        // it without overflowing int arithmetic, which would otherwise surface as an out-of-range
        // slice.
        var topics = new[] { "topic-a" };
        var bytes = new byte[22];
        bytes[0] = 0x02; // version
        bytes[4] = 0x01; // topic count = 1
        bytes[5] = 0x7F; // topic length = int.MaxValue (0x7FFFFFFF)
        bytes[6] = 0xFF;
        bytes[7] = 0xFF;
        bytes[8] = 0xFF;
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_MintedPartitionCountIsNotPositive()
    {
        // arrange
        // A valid single-entry cursor whose minted partition count is overwritten with zero.
        var topics = new[] { "topic-a" };
        var bytes = Format(new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        });
        var mintedPosition = 9 + System.Text.Encoding.UTF8.GetByteCount("topic-a");
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(mintedPosition), 0);
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_MintedPartitionCountExceedsBuffer()
    {
        // arrange
        // A valid single-entry cursor whose minted partition count claims more offsets than the
        // buffer can hold.
        var topics = new[] { "topic-a" };
        var bytes = Format(new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        });
        var mintedPosition = 9 + System.Text.Encoding.UTF8.GetByteCount("topic-a");
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(mintedPosition), 2);
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    public void Parse_Should_Throw_When_OffsetIsOutOfRange(long offset)
    {
        // arrange
        // A negative offset or the long.MaxValue sentinel is never a valid next offset.
        var topics = new[] { "topic-a" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = offset
        };

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_CursorHasTrailingBytes()
    {
        // arrange
        var topics = new[] { "topic-a" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        };
        var bytes = Format(map);
        var padded = new byte[bytes.Length + 1];
        bytes.CopyTo(padded, 0);
        var cursor = Convert.ToBase64String(padded);

        // act
        void Act() => KafkaCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void FormatSingle_Should_MatchGeneralFormat_When_MapHasSinglePartitionZeroEntry()
    {
        // arrange
        // The single-entry fast path must produce byte-identical output to the general codec for a
        // map holding only partition 0, so both paths share one wire format.
        const string topic = "topic-a";
        const long offset = 123456789L;
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition(topic, new Partition(0))] = offset
        };

        // act
        var general = Format(map);
        var single = new byte[KafkaCompositeCursorFormatter.GetSingleFormattedLength(topic)];
        KafkaCompositeCursorFormatter.FormatSingle(topic, offset, single);

        // assert
        Assert.Equal(general, single);
    }

    [Fact]
    public void Parse_Should_RestoreSingleEntry_When_FastPathSerializationRoundTrips()
    {
        // arrange
        // A single-topic resume produced by the fast path must round-trip back through Parse.
        var topics = new[] { "topic-a" };
        const long offset = 42L;
        var buffer = new byte[KafkaCompositeCursorFormatter.GetSingleFormattedLength("topic-a")];
        KafkaCompositeCursorFormatter.FormatSingle("topic-a", offset, buffer);

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Convert.ToBase64String(buffer), topics);

        // assert
        var expected = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = offset
        };
        Assert.Equal(expected, parsed.Offsets);
        Assert.Equal(1, parsed.MintedPartitionCounts["topic-a"]);
    }

    private static string Encode(Dictionary<TopicPartition, long> map)
        => Convert.ToBase64String(Format(map));

    private static byte[] Format(Dictionary<TopicPartition, long> map)
    {
        var buffer = new byte[KafkaCompositeCursorFormatter.GetFormattedLength(map)];
        KafkaCompositeCursorFormatter.Format(map, buffer);
        return buffer;
    }
}
