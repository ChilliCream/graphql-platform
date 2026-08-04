using System.Text;
using System.Threading.Channels;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using HotChocolate.Fusion.Subscriptions;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

public sealed class AzureEventHubsEventStreamBrokerInternalsTests
{
    [Fact]
    public async Task SeedFreshPartitionsAsync_Should_SeedFromBeginning_When_StartFromEarliest()
    {
        // arrange
        var partition = new HubPartition("hub-a", "0");
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "0", isEmpty: false, beginning: 10, last: 50)
        });

        // act
        var result = await AzureEventHubsEventStreamBroker.SeedFreshPartitionsAsync(
            idsSource,
            propertiesSource,
            startFromEarliest: true,
            seedingQueryTimeout: TimeSpan.FromSeconds(1),
            seedingDeadline: TimeSpan.FromSeconds(1),
            ["hub-a"],
            CancellationToken.None);

        // assert
        Assert.Equal(10L, result.CursorMap[partition]);
        Assert.True(result.PerHubKnownIds["hub-a"].SetEquals(["0"]));
    }

    [Fact]
    public async Task SeedFreshPartitionsAsync_Should_SeedFromLastPlusOne_When_NotStartFromEarliest()
    {
        // arrange
        var partition = new HubPartition("hub-a", "0");
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "0", isEmpty: false, beginning: 10, last: 50)
        });

        // act
        var result = await AzureEventHubsEventStreamBroker.SeedFreshPartitionsAsync(
            idsSource,
            propertiesSource,
            startFromEarliest: false,
            seedingQueryTimeout: TimeSpan.FromSeconds(1),
            seedingDeadline: TimeSpan.FromSeconds(1),
            ["hub-a"],
            CancellationToken.None);

        // assert
        Assert.Equal(51L, result.CursorMap[partition]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SeedFreshPartitionsAsync_Should_SeedToZero_When_PartitionEmpty(
        bool startFromEarliest)
    {
        // arrange
        var partition = new HubPartition("hub-a", "0");
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "0", isEmpty: true, beginning: -1, last: -1)
        });

        // act
        var result = await AzureEventHubsEventStreamBroker.SeedFreshPartitionsAsync(
            idsSource,
            propertiesSource,
            startFromEarliest,
            seedingQueryTimeout: TimeSpan.FromSeconds(1),
            seedingDeadline: TimeSpan.FromSeconds(1),
            ["hub-a"],
            CancellationToken.None);

        // assert
        Assert.Equal(0L, result.CursorMap[partition]);
        Assert.Equal(EventPosition.Earliest, result.StartPositions[partition]);
    }

    [Fact]
    public async Task SeedFreshPartitionsAsync_Should_Throw_When_PropertiesNeverAvailable()
    {
        // arrange
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new ThrowingPropertiesSource();

        // act
        async Task Act()
            => await AzureEventHubsEventStreamBroker.SeedFreshPartitionsAsync(
                idsSource,
                propertiesSource,
                startFromEarliest: true,
                seedingQueryTimeout: TimeSpan.FromMilliseconds(5),
                seedingDeadline: TimeSpan.FromMilliseconds(20),
                ["hub-a"],
                CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<EventStreamSeedingException>(Act);
    }

    [Fact]
    public void ResolveStartPosition_Should_ReturnEarliest_When_PartitionEmpty()
    {
        // arrange
        // an empty partition has no event at its baseline sequence number to address

        // act
        var position = AzureEventHubsEventStreamBroker.ResolveStartPosition(
            isEmpty: true,
            sequenceNumber: 7);

        // assert
        Assert.Equal(EventPosition.Earliest, position);
    }

    [Fact]
    public void ResolveStartPosition_Should_ReturnInclusiveSequence_When_PartitionNotEmpty()
    {
        // arrange

        // act
        var position = AzureEventHubsEventStreamBroker.ResolveStartPosition(
            isEmpty: false,
            sequenceNumber: 7);

        // assert
        Assert.Equal(EventPosition.FromSequenceNumber(7, isInclusive: true), position);
    }

    [Fact]
    public async Task ResolveResumeAsync_Should_ResumeFromStoredNext_When_CursorValid()
    {
        // arrange
        var partition = new HubPartition("hub-a", "0");
        var resumeState = new AzureEventHubsResumeState
        {
            NextSequenceNumbers = new Dictionary<HubPartition, long> { [partition] = 5 },
            MintedPartitionIds = new Dictionary<string, IReadOnlySet<string>>
            {
                ["hub-a"] = new HashSet<string> { "0" }
            }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "0", isEmpty: false, beginning: 0, last: 10)
        });

        // act
        var result = await AzureEventHubsEventStreamBroker.ResolveResumeAsync(
            idsSource,
            propertiesSource,
            resumeState,
            seedingQueryTimeout: TimeSpan.FromSeconds(1),
            seedingDeadline: TimeSpan.FromSeconds(1),
            ["hub-a"],
            CancellationToken.None);

        // assert
        Assert.Equal(5L, result.CursorMap[partition]);
    }

    [Fact]
    public async Task ResolveResumeAsync_Should_Throw_When_NextBelowBeginning()
    {
        // arrange
        var partition = new HubPartition("hub-a", "0");
        var resumeState = new AzureEventHubsResumeState
        {
            NextSequenceNumbers = new Dictionary<HubPartition, long> { [partition] = 2 },
            MintedPartitionIds = new Dictionary<string, IReadOnlySet<string>>
            {
                ["hub-a"] = new HashSet<string> { "0" }
            }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "0", isEmpty: false, beginning: 5, last: 10)
        });

        // act
        async Task Act()
            => await AzureEventHubsEventStreamBroker.ResolveResumeAsync(
                idsSource,
                propertiesSource,
                resumeState,
                seedingQueryTimeout: TimeSpan.FromSeconds(1),
                seedingDeadline: TimeSpan.FromSeconds(1),
                ["hub-a"],
                CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public async Task ResolveResumeAsync_Should_Throw_When_NextBeyondLastPlusOne()
    {
        // arrange
        var partition = new HubPartition("hub-a", "0");
        var resumeState = new AzureEventHubsResumeState
        {
            NextSequenceNumbers = new Dictionary<HubPartition, long> { [partition] = 100 },
            MintedPartitionIds = new Dictionary<string, IReadOnlySet<string>>
            {
                ["hub-a"] = new HashSet<string> { "0" }
            }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "0", isEmpty: false, beginning: 0, last: 10)
        });

        // act
        async Task Act()
            => await AzureEventHubsEventStreamBroker.ResolveResumeAsync(
                idsSource,
                propertiesSource,
                resumeState,
                seedingQueryTimeout: TimeSpan.FromSeconds(1),
                seedingDeadline: TimeSpan.FromSeconds(1),
                ["hub-a"],
                CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public async Task ResolveResumeAsync_Should_Throw_When_MintedPartitionAbsentFromLive()
    {
        // arrange
        var partition0 = new HubPartition("hub-a", "0");
        var partition1 = new HubPartition("hub-a", "1");
        var resumeState = new AzureEventHubsResumeState
        {
            NextSequenceNumbers = new Dictionary<HubPartition, long>
            {
                [partition0] = 5,
                [partition1] = 5
            },
            MintedPartitionIds = new Dictionary<string, IReadOnlySet<string>>
            {
                ["hub-a"] = new HashSet<string> { "0", "1" }
            }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>());

        // act
        async Task Act()
            => await AzureEventHubsEventStreamBroker.ResolveResumeAsync(
                idsSource,
                propertiesSource,
                resumeState,
                seedingQueryTimeout: TimeSpan.FromSeconds(1),
                seedingDeadline: TimeSpan.FromSeconds(1),
                ["hub-a"],
                CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public async Task ResolveResumeAsync_Should_FoldGrownPartitionFromBeginning()
    {
        // arrange
        var partition0 = new HubPartition("hub-a", "0");
        var partition1 = new HubPartition("hub-a", "1");
        var resumeState = new AzureEventHubsResumeState
        {
            NextSequenceNumbers = new Dictionary<HubPartition, long> { [partition0] = 5 },
            MintedPartitionIds = new Dictionary<string, IReadOnlySet<string>>
            {
                ["hub-a"] = new HashSet<string> { "0" }
            }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0", "1"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition0] = Props("hub-a", "0", isEmpty: false, beginning: 0, last: 10),
            [partition1] = Props("hub-a", "1", isEmpty: false, beginning: 7, last: 20)
        });

        // act
        var result = await AzureEventHubsEventStreamBroker.ResolveResumeAsync(
            idsSource,
            propertiesSource,
            resumeState,
            seedingQueryTimeout: TimeSpan.FromSeconds(1),
            seedingDeadline: TimeSpan.FromSeconds(1),
            ["hub-a"],
            CancellationToken.None);

        // assert
        Assert.Equal(5L, result.CursorMap[partition0]);
        Assert.Equal(7L, result.CursorMap[partition1]);
    }

    [Fact]
    public async Task RunDiscoveryTickAsync_Should_EmitDiscoveredPartition_When_NewPartitionAppears()
    {
        // arrange
        var partition = new HubPartition("hub-a", "2");
        var knownIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["hub-a"] = new HashSet<string>(StringComparer.Ordinal) { "0", "1" }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0", "1", "2"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>
        {
            [partition] = Props("hub-a", "2", isEmpty: false, beginning: 3, last: 9)
        });
        var channel = Channel.CreateUnbounded<AzureEventHubsEventStreamBroker.AggregatorItem>();

        // act
        var fault = await AzureEventHubsEventStreamBroker.RunDiscoveryTickAsync(
            ["hub-a"],
            idsSource,
            propertiesSource,
            knownIds,
            channel.Writer,
            CancellationToken.None);

        // assert
        Assert.Null(fault);
        Assert.True(channel.Reader.TryRead(out var item));
        var discovered = Assert.IsType<AzureEventHubsEventStreamBroker.PartitionDiscoveredItem>(item);
        Assert.Equal(("hub-a", "2", 3L), (discovered.Hub, discovered.PartitionId, discovered.Baseline));
        Assert.True(knownIds["hub-a"].SetEquals(["0", "1", "2"]));
    }

    [Fact]
    public async Task RunDiscoveryTickAsync_Should_EmitNothing_When_NoNewPartition()
    {
        // arrange
        var knownIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["hub-a"] = new HashSet<string>(StringComparer.Ordinal) { "0", "1" }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0", "1"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>());
        var channel = Channel.CreateUnbounded<AzureEventHubsEventStreamBroker.AggregatorItem>();

        // act
        var fault = await AzureEventHubsEventStreamBroker.RunDiscoveryTickAsync(
            ["hub-a"],
            idsSource,
            propertiesSource,
            knownIds,
            channel.Writer,
            CancellationToken.None);

        // assert
        Assert.Null(fault);
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RunDiscoveryTickAsync_Should_ReturnFault_When_KnownPartitionDisappears()
    {
        // arrange
        var knownIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["hub-a"] = new HashSet<string>(StringComparer.Ordinal) { "0", "1" }
        };
        var idsSource = new StaticIdsSource(new Dictionary<string, string[]>
        {
            ["hub-a"] = ["0"]
        });
        var propertiesSource = new StaticPropertiesSource(new Dictionary<HubPartition, PartitionProperties>());
        var channel = Channel.CreateUnbounded<AzureEventHubsEventStreamBroker.AggregatorItem>();

        // act
        var fault = await AzureEventHubsEventStreamBroker.RunDiscoveryTickAsync(
            ["hub-a"],
            idsSource,
            propertiesSource,
            knownIds,
            channel.Writer,
            CancellationToken.None);

        // assert
        Assert.IsType<InvalidOperationException>(fault);
    }

    [Fact]
    public void TryFoldDiscoveredPartition_Should_FoldOnceThenBeIdempotent()
    {
        // arrange
        var partition = new HubPartition("hub-a", "2");
        var cursorMap = new Dictionary<HubPartition, long>();
        var startedPartitions = new HashSet<HubPartition>();

        // act
        var first = AzureEventHubsEventStreamBroker.TryFoldDiscoveredPartition(
            cursorMap,
            startedPartitions,
            "hub-a",
            "2",
            baseline: 3);
        var second = AzureEventHubsEventStreamBroker.TryFoldDiscoveredPartition(
            cursorMap,
            startedPartitions,
            "hub-a",
            "2",
            baseline: 99);

        // assert
        Assert.True(first);
        Assert.False(second);
        Assert.Equal(3L, cursorMap[partition]);
    }

    [Fact]
    public async Task EmitAsync_Should_AdvanceMapAndEmitCursor_When_Delivered()
    {
        // arrange
        var channel = AzureEventHubsEventStreamOptions.CreateDefaultMessageChannel();
        var partition = new HubPartition("hub-a", "0");
        var cursorMap = new Dictionary<HubPartition, long>();

        // act
        var outcome = await AzureEventHubsEventStreamBroker.EmitAsync(
            channel.Writer,
            cursorMap,
            emitCursor: true,
            "hub-a",
            "0",
            sequenceNumber: 5,
            """{"id":1}"""u8.ToArray(),
            CancellationToken.None);

        // assert
        Assert.Equal(AzureEventHubsEventStreamBroker.WriteOutcome.Delivered, outcome);
        Assert.Equal(6L, cursorMap[partition]);
        Assert.True(channel.Reader.TryRead(out var message));
        using (message)
        {
            Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
            Assert.Equal(6L, ParseCursor(message.Cursor, ["hub-a"]).NextSequenceNumbers[partition]);
        }
    }

    [Fact]
    public async Task EmitAsync_Should_NotAdvance_When_ChannelClosed()
    {
        // arrange
        var channel = AzureEventHubsEventStreamOptions.CreateDefaultMessageChannel();
        channel.Writer.Complete();
        var partition = new HubPartition("hub-a", "0");
        var cursorMap = new Dictionary<HubPartition, long>();

        // act
        var outcome = await AzureEventHubsEventStreamBroker.EmitAsync(
            channel.Writer,
            cursorMap,
            emitCursor: true,
            "hub-a",
            "0",
            sequenceNumber: 5,
            """{"id":1}"""u8.ToArray(),
            CancellationToken.None);

        // assert
        Assert.Equal(AzureEventHubsEventStreamBroker.WriteOutcome.Closed, outcome);
        Assert.False(cursorMap.ContainsKey(partition));
    }

    [Fact]
    public async Task EmitAsync_Should_AdvanceButSuppressCursor_When_EmitCursorFalse()
    {
        // arrange
        var channel = AzureEventHubsEventStreamOptions.CreateDefaultMessageChannel();
        var partition = new HubPartition("hub-a", "0");
        var cursorMap = new Dictionary<HubPartition, long> { [partition] = 5 };

        // act
        var outcome = await AzureEventHubsEventStreamBroker.EmitAsync(
            channel.Writer,
            cursorMap,
            emitCursor: false,
            "hub-a",
            "0",
            sequenceNumber: 5,
            """{"id":1}"""u8.ToArray(),
            CancellationToken.None);

        // assert
        Assert.Equal(AzureEventHubsEventStreamBroker.WriteOutcome.Delivered, outcome);
        Assert.Equal(6L, cursorMap[partition]);
        Assert.True(channel.Reader.TryRead(out var message));
        using (message)
        {
            Assert.True(message.Cursor.IsEmpty);
        }
    }

    [Fact]
    public async Task EmitAsync_Should_KeepPerPartitionMonotonic_When_TwoPartitionsInterleave()
    {
        // arrange
        var channel = AzureEventHubsEventStreamOptions.CreateDefaultMessageChannel();
        var partition0 = new HubPartition("hub-a", "0");
        var partition1 = new HubPartition("hub-a", "1");
        var cursorMap = new Dictionary<HubPartition, long>
        {
            [partition0] = 0,
            [partition1] = 0
        };

        // act
        _ = await AzureEventHubsEventStreamBroker.EmitAsync(
            channel.Writer,
            cursorMap,
            emitCursor: true,
            "hub-a",
            "0",
            sequenceNumber: 5,
            """{"id":1}"""u8.ToArray(),
            CancellationToken.None);
        _ = await AzureEventHubsEventStreamBroker.EmitAsync(
            channel.Writer,
            cursorMap,
            emitCursor: true,
            "hub-a",
            "1",
            sequenceNumber: 3,
            """{"id":2}"""u8.ToArray(),
            CancellationToken.None);
        _ = await AzureEventHubsEventStreamBroker.EmitAsync(
            channel.Writer,
            cursorMap,
            emitCursor: true,
            "hub-a",
            "0",
            sequenceNumber: 6,
            """{"id":3}"""u8.ToArray(),
            CancellationToken.None);
        var messages = ReadMessages(channel.Reader, count: 3);
        var parsed = ParseCursor(messages[^1].Cursor, ["hub-a"]);

        // assert
        try
        {
            Assert.Equal(7L, cursorMap[partition0]);
            Assert.Equal(4L, cursorMap[partition1]);
            Assert.Equal(
                new Dictionary<HubPartition, long>
                {
                    [partition0] = 7,
                    [partition1] = 4
                },
                parsed.NextSequenceNumbers);
        }
        finally
        {
            DisposeMessages(messages);
        }
    }

    private static AzureEventHubsResumeState ParseCursor(
        ReadOnlySpan<byte> cursor,
        string[] topics)
        => AzureEventHubsCompositeCursorFormatter.Parse(Encoding.UTF8.GetString(cursor), topics);

    private static List<EventMessage> ReadMessages(
        ChannelReader<EventMessage> reader,
        int count)
    {
        var messages = new List<EventMessage>(count);

        for (var i = 0; i < count; i++)
        {
            if (!reader.TryRead(out var message))
            {
                throw new InvalidOperationException("Expected event message was not available.");
            }

            messages.Add(message);
        }

        return messages;
    }

    private static void DisposeMessages(List<EventMessage> messages)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            messages[i].Dispose();
        }
    }

    private static PartitionProperties Props(
        string hub,
        string id,
        bool isEmpty,
        long beginning,
        long last)
        => EventHubsModelFactory.PartitionProperties(
            hub,
            id,
            isEmpty,
            beginning,
            last,
            "0",
            default);

    private sealed class StaticIdsSource(Dictionary<string, string[]> idsByHub) : IPartitionIdsSource
    {
        public Task<string[]> GetPartitionIdsAsync(
            string hub,
            CancellationToken cancellationToken)
            => Task.FromResult(idsByHub[hub]);
    }

    private sealed class ThrowingIdsSource : IPartitionIdsSource
    {
        public Task<string[]> GetPartitionIdsAsync(
            string hub,
            CancellationToken cancellationToken)
            => throw new TimeoutException();
    }

    private sealed class StaticPropertiesSource(Dictionary<HubPartition, PartitionProperties> props)
        : IPartitionPropertiesSource
    {
        public Task<PartitionProperties> GetPartitionPropertiesAsync(
            string hub,
            string partitionId,
            CancellationToken cancellationToken)
            => Task.FromResult(props[new HubPartition(hub, partitionId)]);
    }

    private sealed class ThrowingPropertiesSource : IPartitionPropertiesSource
    {
        public Task<PartitionProperties> GetPartitionPropertiesAsync(
            string hub,
            string partitionId,
            CancellationToken cancellationToken)
            => throw new TimeoutException();
    }
}
