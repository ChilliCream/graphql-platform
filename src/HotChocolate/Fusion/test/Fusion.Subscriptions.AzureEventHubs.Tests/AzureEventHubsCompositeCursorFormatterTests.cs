using System.Buffers.Binary;
using System.Text;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

public sealed class AzureEventHubsCompositeCursorFormatterTests
{
    [Fact]
    public void Parse_Should_RestoreState_When_SingleHubSinglePartitionRoundTrips()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var map = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 42
        };

        // act
        var parsed = AzureEventHubsCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.NextSequenceNumbers);
        Assert.True(parsed.MintedPartitionIds["hub-a"].SetEquals(["0"]));
    }

    [Fact]
    public void Parse_Should_RestoreState_When_SingleHubMultiPartitionRoundTrips()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var map = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1,
            [new HubPartition("hub-a", "1")] = 2,
            [new HubPartition("hub-a", "2")] = 3
        };

        // act
        var parsed = AzureEventHubsCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.NextSequenceNumbers);
        Assert.True(parsed.MintedPartitionIds["hub-a"].SetEquals(["0", "1", "2"]));
    }

    [Fact]
    public void Parse_Should_RestoreState_When_MultiHubMultiPartitionRoundTrips()
    {
        // arrange
        var topics = new[] { "hub-a", "hub-b" };
        var map = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 5,
            [new HubPartition("hub-a", "1")] = 9,
            [new HubPartition("hub-b", "a")] = 0,
            [new HubPartition("hub-b", "b")] = 100,
            [new HubPartition("hub-b", "c")] = 123456789
        };

        // act
        var parsed = AzureEventHubsCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.NextSequenceNumbers);
        Assert.True(parsed.MintedPartitionIds["hub-a"].SetEquals(["0", "1"]));
        Assert.True(parsed.MintedPartitionIds["hub-b"].SetEquals(["a", "b", "c"]));
    }

    [Fact]
    public void Parse_Should_RestoreState_When_PartitionIdsAreOpaqueNonNumeric()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var map = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "p-xyz")] = 12,
            [new HubPartition("hub-a", "p-abc")] = 34
        };

        // act
        var parsed = AzureEventHubsCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Equal(map, parsed.NextSequenceNumbers);
        Assert.True(parsed.MintedPartitionIds["hub-a"].SetEquals(["p-xyz", "p-abc"]));
    }

    [Fact]
    public void Format_Should_BeByteIdentical_When_MapInsertedOutOfOrder()
    {
        // arrange
        var topics = new[] { "hub-a", "hub-b" };
        var ascending = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 10,
            [new HubPartition("hub-a", "1")] = 20,
            [new HubPartition("hub-b", "a")] = 30,
            [new HubPartition("hub-b", "b")] = 40
        };
        var shuffled = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-b", "b")] = 40,
            [new HubPartition("hub-a", "1")] = 20,
            [new HubPartition("hub-b", "a")] = 30,
            [new HubPartition("hub-a", "0")] = 10
        };

        // act
        var parsed = AzureEventHubsCompositeCursorFormatter.Parse(Encode(shuffled), topics);

        // assert
        Assert.Equal(Format(ascending), Format(shuffled));
        Assert.Equal(shuffled, parsed.NextSequenceNumbers);
    }

    [Fact]
    public void Parse_Should_Throw_When_CursorIsNotBase64()
    {
        // arrange
        var topics = new[] { "hub-a" };

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse("not-base64", topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_VersionIsUnknown()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });
        bytes[0] = 0x02;
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_HubCountExceedsBufferCapacity()
    {
        // arrange
        var topics = new[] { "hub-a" };
        Span<byte> bytes = [0x01, 0x7F, 0xFF, 0xFF, 0xFF];
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_HubNameLengthOverflowsGuard()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = new byte[27];
        bytes[0] = 0x01;
        bytes[4] = 0x01;
        bytes[5] = 0x7F;
        bytes[6] = 0xFF;
        bytes[7] = 0xFF;
        bytes[8] = 0xFF;
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_HubIsNotInSubscription()
    {
        // arrange
        var cursor = Encode(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, ["hub-b"]);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_HubNameHasInvalidUtf8()
    {
        // arrange
        var topics = new[] { "ab" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("ab", "0")] = 1
        });
        bytes[9] = 0xFF;
        bytes[10] = 0xFF;
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_HubIsDuplicated()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var single = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });
        var entry = single.AsSpan(5).ToArray();
        var bytes = new byte[5 + (entry.Length * 2)];
        bytes[0] = 0x01;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(1), 2);
        entry.CopyTo(bytes.AsSpan(5));
        entry.CopyTo(bytes.AsSpan(5 + entry.Length));
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_PartitionCountIsNotPositive()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });
        var partitionCountPosition = 9 + Encoding.UTF8.GetByteCount("hub-a");
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(partitionCountPosition), 0);
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_PartitionCountExceedsBuffer()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });
        var partitionCountPosition = 9 + Encoding.UTF8.GetByteCount("hub-a");
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(partitionCountPosition), 2);
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_PartitionIdLengthOverflowsGuard()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });
        var idLengthPosition = 13 + Encoding.UTF8.GetByteCount("hub-a");
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(idLengthPosition), int.MaxValue);
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_PartitionIdHasInvalidUtf8()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "ab")] = 1
        });
        var idBytesPosition = 17 + Encoding.UTF8.GetByteCount("hub-a");
        bytes[idBytesPosition] = 0xFF;
        bytes[idBytesPosition + 1] = 0xFF;
        var cursor = Convert.ToBase64String(bytes);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_PartitionIdIsDuplicatedWithinHub()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var cursor = Convert.ToBase64String(CreateDuplicatePartitionIdCursor("hub-a", "0"));

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    public void Parse_Should_Throw_When_NextSequenceNumberIsOutOfRange(long nextSequenceNumber)
    {
        // arrange
        var topics = new[] { "hub-a" };
        var map = new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = nextSequenceNumber
        };

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(Encode(map), topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_CursorHasTrailingBytes()
    {
        // arrange
        var topics = new[] { "hub-a" };
        var bytes = Format(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });
        var padded = new byte[bytes.Length + 1];
        bytes.CopyTo(padded, 0);
        var cursor = Convert.ToBase64String(padded);

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_SubscriptionHubMissingFromCursor()
    {
        // arrange
        var topics = new[] { "hub-a", "hub-b" };
        var cursor = Encode(new Dictionary<HubPartition, long>
        {
            [new HubPartition("hub-a", "0")] = 1
        });

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, topics);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_LegacyCursorIsProvided()
    {
        // arrange
        var cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes("0:0"));

        // act
        void Act() => AzureEventHubsCompositeCursorFormatter.Parse(cursor, ["hub-a"]);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    private static string Encode(Dictionary<HubPartition, long> map)
        => Convert.ToBase64String(Format(map));

    private static byte[] Format(Dictionary<HubPartition, long> map)
    {
        var buffer = new byte[AzureEventHubsCompositeCursorFormatter.GetFormattedLength(map)];
        AzureEventHubsCompositeCursorFormatter.Format(map, buffer);
        return buffer;
    }

    private static byte[] CreateDuplicatePartitionIdCursor(string hub, string partitionId)
    {
        var hubBytes = Encoding.UTF8.GetBytes(hub);
        var partitionIdBytes = Encoding.UTF8.GetBytes(partitionId);
        var partitionEntryLength = 4 + partitionIdBytes.Length + 8;
        var bytes = new byte[5 + 4 + hubBytes.Length + 4 + (partitionEntryLength * 2)];
        var position = 0;

        bytes[position] = 0x01;
        position++;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(position), 1);
        position += 4;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(position), hubBytes.Length);
        position += 4;
        hubBytes.CopyTo(bytes.AsSpan(position));
        position += hubBytes.Length;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(position), 2);
        position += 4;

        for (var i = 0; i < 2; i++)
        {
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(position), partitionIdBytes.Length);
            position += 4;
            partitionIdBytes.CopyTo(bytes.AsSpan(position));
            position += partitionIdBytes.Length;
            BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(position), i + 1);
            position += 8;
        }

        return bytes;
    }
}
