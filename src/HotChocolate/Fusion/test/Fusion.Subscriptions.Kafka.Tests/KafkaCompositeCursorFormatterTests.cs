using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaCompositeCursorFormatterTests
{
    [Fact]
    public void Parse_Should_RestoreMap_When_MultiTopicMultiPartitionCursorRoundTrips()
    {
        // arrange
        // A cursor spanning two topics, including two partitions within one topic.
        var topics = new[] { "topic-a", "topic-b" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 5,
            [new TopicPartition("topic-a", new Partition(1))] = 9,
            [new TopicPartition("topic-b", new Partition(0))] = 0,
            [new TopicPartition("topic-b", new Partition(3))] = 123456789
        };

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed);
    }

    [Fact]
    public void Parse_Should_RestoreMap_When_SingleEntryCursorRoundTrips()
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
        Assert.Equal(map, parsed);
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

    [Fact]
    public void Parse_Should_Throw_When_VersionIsUnknown()
    {
        // arrange
        var topics = new[] { "topic-a" };
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", new Partition(0))] = 1
        };
        var bytes = Format(map);
        bytes[0] = 0x02;
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
    public void Parse_Should_Throw_When_CountExceedsBufferCapacity()
    {
        // arrange
        // A tiny cursor whose count field claims int.MaxValue entries. A naive parser would
        // pre-size a dictionary for billions of entries and OOM; the count must be rejected first.
        var topics = new[] { "topic-a" };
        Span<byte> bytes = [0x01, 0x7F, 0xFF, 0xFF, 0xFF];
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
        // A single-entry cursor whose topic length is near int.MaxValue. The length guard must
        // reject it without overflowing int arithmetic, which would otherwise slip past the check
        // and surface as an ArgumentOutOfRangeException from the topic slice.
        var topics = new[] { "topic-a" };
        var bytes = new byte[22];
        bytes[0] = 0x01; // version
        bytes[4] = 0x01; // count = 1
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
    public void FormatSingle_Should_MatchGeneralFormat_When_MapHasSingleEntry()
    {
        // arrange
        // The single-entry fast path must produce byte-identical output to the general codec so that
        // there is one wire format and a resume cursor interoperates between the two.
        const string topic = "topic-a";
        var partition = new Partition(3);
        const long offset = 123456789L;
        var map = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition(topic, partition)] = offset
        };

        // act
        var general = Format(map);
        var single = new byte[KafkaCompositeCursorFormatter.GetSingleFormattedLength(topic)];
        KafkaCompositeCursorFormatter.FormatSingle(topic, partition, offset, single);

        // assert
        Assert.Equal(general, single);
    }

    [Fact]
    public void Parse_Should_RestoreSingleEntry_When_FastPathSerializationRoundTrips()
    {
        // arrange
        // A single-topic resume produced by the fast path must round-trip back through Parse.
        var topics = new[] { "topic-a" };
        var partition = new Partition(0);
        const long offset = 42L;
        var buffer = new byte[KafkaCompositeCursorFormatter.GetSingleFormattedLength("topic-a")];
        KafkaCompositeCursorFormatter.FormatSingle("topic-a", partition, offset, buffer);

        // act
        var parsed = KafkaCompositeCursorFormatter.Parse(Convert.ToBase64String(buffer), topics);

        // assert
        var expected = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-a", partition)] = offset
        };
        Assert.Equal(expected, parsed);
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
