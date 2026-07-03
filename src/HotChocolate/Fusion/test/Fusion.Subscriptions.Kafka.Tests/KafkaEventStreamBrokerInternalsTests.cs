using System.Linq;
using System.Text;
using Confluent.Kafka;
using HotChocolate.Fusion.Subscriptions;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaEventStreamBrokerInternalsTests
{
    [Fact]
    public async Task EmitMessageAsync_Should_CommitAdvanceOnPumpThread_When_WriteSucceeds()
    {
        // arrange
        // A successful channel write is the sole signal that advances the committed cursor. The
        // commit happens inline on the calling (pump) thread with no reader draining the channel, so
        // the outcome is deterministic and cannot race a consumer.
        var channel = KafkaEventStreamOptions.CreateDefaultMessageChannel();
        var cursorMap = new Dictionary<TopicPartition, long>();
        var cursorCache = new SingleEntryCursorCache();
        var partition = new TopicPartition("topic-a", new Partition(0));

        // act
        var outcome = await KafkaEventStreamBroker.EmitMessageAsync(
            channel.Writer, cursorMap, cursorCache, emitCursor: true, partition, offset: 5,
            """{"id":1}"""u8.ToArray(), CancellationToken.None);

        // assert
        Assert.Equal(KafkaEventStreamBroker.WriteOutcome.Delivered, outcome);
        Assert.Equal(6L, cursorMap[partition]);
        Assert.True(channel.Reader.TryRead(out var delivered));
        using (delivered)
        {
            Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(delivered!.Body));
            var state = KafkaCompositeCursorFormatter.Parse(
                Encoding.UTF8.GetString(delivered.Cursor), ["topic-a"]);
            Assert.Equal(6L, state.Offsets[partition]);
        }
    }

    [Fact]
    public async Task EmitMessageAsync_Should_NotCommitAdvance_When_ChannelIsClosed()
    {
        // arrange
        // A completed channel cannot accept the write, so the pump must not advance the committed
        // cursor for a message it could not deliver.
        var channel = KafkaEventStreamOptions.CreateDefaultMessageChannel();
        channel.Writer.Complete();
        var cursorMap = new Dictionary<TopicPartition, long>();
        var cursorCache = new SingleEntryCursorCache();
        var partition = new TopicPartition("topic-a", new Partition(0));

        // act
        var outcome = await KafkaEventStreamBroker.EmitMessageAsync(
            channel.Writer, cursorMap, cursorCache, emitCursor: true, partition, offset: 5,
            """{"id":1}"""u8.ToArray(), CancellationToken.None);

        // assert
        Assert.Equal(KafkaEventStreamBroker.WriteOutcome.Closed, outcome);
        Assert.False(cursorMap.ContainsKey(partition));
    }

    [Fact]
    public async Task EmitMessageAsync_Should_AdvanceMapButSuppressCursor_When_CursorNotRequired()
    {
        // arrange
        // An inbound-only resume tracks progress in the cursor map to survive rebalances, but the
        // stream exposes no output cursor, so emitCursor is false: the delivered message carries an
        // empty cursor while the progress map still advances past the delivered offset.
        var channel = KafkaEventStreamOptions.CreateDefaultMessageChannel();
        var partition = new TopicPartition("topic-a", new Partition(0));
        var cursorMap = new Dictionary<TopicPartition, long> { [partition] = 5 };

        // act
        var outcome = await KafkaEventStreamBroker.EmitMessageAsync(
            channel.Writer, cursorMap, cursorCache: null, emitCursor: false, partition, offset: 5,
            """{"id":1}"""u8.ToArray(), CancellationToken.None);

        // assert
        Assert.Equal(KafkaEventStreamBroker.WriteOutcome.Delivered, outcome);
        Assert.Equal(6L, cursorMap[partition]);
        Assert.True(channel.Reader.TryRead(out var delivered));
        using (delivered)
        {
            Assert.True(delivered!.Cursor.IsEmpty);
        }
    }

    [Fact]
    public void ResolveStartOffsets_Should_ResumeFromTrackedOffsetWithoutQuery_When_PartitionAlreadyTracked()
    {
        // arrange
        // A partition already present in the live cursor map resumes from its stored next offset. The
        // watermark source throws if queried, proving a tracked partition is never re-seeded forward.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var cursorMap = new Dictionary<TopicPartition, long> { [partition] = 42 };

        // act
        var result = Resolve(
            new ThrowingWatermarkSource(), [partition], resumeState: null, cursorMap,
            AutoOffsetReset.Latest);

        // assert
        Assert.Equal(new Offset(42), result[partition]);
    }

    [Fact]
    public void ResolveStartOffsets_Should_PreferLiveMap_When_BothResumeAndLiveMapContainPartition()
    {
        // arrange
        // The live cursor map holds the position reached this session and takes precedence over the
        // resume cursor, so a rebalance resumes from live progress rather than rewinding.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeState = new KafkaResumeState
        {
            Offsets = new Dictionary<TopicPartition, long> { [partition] = 7 },
            MintedPartitionCounts = new Dictionary<string, int> { ["topic-a"] = 1 }
        };
        var cursorMap = new Dictionary<TopicPartition, long> { [partition] = 42 };

        // act
        var result = Resolve(
            new ThrowingWatermarkSource(), [partition], resumeState, cursorMap,
            AutoOffsetReset.Latest);

        // assert
        Assert.Equal(new Offset(42), result[partition]);
    }

    [Theory]
    [InlineData(AutoOffsetReset.Latest)]
    [InlineData(AutoOffsetReset.Earliest)]
    public void ResolveStartOffsets_Should_StartGrownPartitionAtBeginning_When_ResumeMintedFewerPartitions(
        AutoOffsetReset autoOffsetReset)
    {
        // arrange
        // The resume cursor minted one partition; a second partition has since been added. The grown
        // partition starts at the beginning regardless of the reset mode, and the tracked partition
        // keeps its stored offset.
        var p0 = new TopicPartition("topic-a", new Partition(0));
        var p1 = new TopicPartition("topic-a", new Partition(1));
        var resumeState = new KafkaResumeState
        {
            Offsets = new Dictionary<TopicPartition, long> { [p0] = 7 },
            MintedPartitionCounts = new Dictionary<string, int> { ["topic-a"] = 1 }
        };
        var cursorMap = new Dictionary<TopicPartition, long>(resumeState.Offsets);

        // act
        var result = Resolve(
            new ThrowingWatermarkSource(), [p0, p1], resumeState, cursorMap, autoOffsetReset);

        // assert
        Assert.Equal(new Offset(7), result[p0]);
        Assert.Equal(Offset.Beginning, result[p1]);
        Assert.Equal(0L, cursorMap[p1]);
    }

    [Theory]
    [InlineData(AutoOffsetReset.Latest, 50L)]
    [InlineData(AutoOffsetReset.Earliest, 10L)]
    public void ResolveStartOffsets_Should_SeedFromWatermark_When_FreshCursorTrackedSubscribe(
        AutoOffsetReset autoOffsetReset,
        long expectedBaseline)
    {
        // arrange
        // A fresh cursor-tracked subscribe with no resume seeds each partition from the broker
        // watermark: the live end (High) under Latest, the earliest retained event (Low) under
        // Earliest.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var cursorMap = new Dictionary<TopicPartition, long>();
        var watermarks = new StubWatermarkSource(new WatermarkOffsets(new Offset(10), new Offset(50)));

        // act
        var result = Resolve(watermarks, [partition], resumeState: null, cursorMap, autoOffsetReset);

        // assert
        Assert.Equal(new Offset(expectedBaseline), result[partition]);
        Assert.Equal(expectedBaseline, cursorMap[partition]);
    }

    [Fact]
    public void ResolveStartOffsets_Should_LeaveUnset_When_NonResumableSubscription()
    {
        // arrange
        // A non-resumable subscription tracks nothing, so every partition falls back to the
        // configured AutoOffsetReset at the transport.
        var partition = new TopicPartition("topic-a", new Partition(0));

        // act
        var result = Resolve(
            new ThrowingWatermarkSource(), [partition], resumeState: null, cursorMap: null,
            AutoOffsetReset.Latest);

        // assert
        Assert.Equal(Offset.Unset, result[partition]);
    }

    [Fact]
    public void ResolveStartOffsets_Should_Throw_When_SeedingDeadlineElapsesWithoutWatermark()
    {
        // arrange
        // Every watermark query fails and the seeding deadline is tiny, so no partition can be
        // baselined and the subscription fails rather than start from an incomplete cursor.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var cursorMap = new Dictionary<TopicPartition, long>();

        // act
        void Act() => KafkaEventStreamBroker.ResolveStartOffsets(
            new ThrowingWatermarkSource(), [partition], resumeState: null, cursorMap,
            AutoOffsetReset.Latest, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(20))
            .ToList();

        // assert
        Assert.Throws<EventStreamSeedingException>(Act);
    }

    [Fact]
    public void ValidateNoPartitionShrink_Should_Throw_When_TopicLostPartitions()
    {
        // arrange
        // The cursor minted three partitions but only two are live, so the positional cursor can no
        // longer be honored.
        var resumeState = new KafkaResumeState
        {
            Offsets = new Dictionary<TopicPartition, long>(),
            MintedPartitionCounts = new Dictionary<string, int> { ["topic-a"] = 3 }
        };
        var assigned = new List<TopicPartition>
        {
            new("topic-a", new Partition(0)),
            new("topic-a", new Partition(1))
        };

        // act
        void Act() => KafkaEventStreamBroker.ValidateNoPartitionShrink(assigned, resumeState);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void ValidateNoPartitionShrink_Should_Throw_When_TopicDeleted()
    {
        // arrange
        // The topic the cursor minted has no live partitions at all, meaning it was deleted.
        var resumeState = new KafkaResumeState
        {
            Offsets = new Dictionary<TopicPartition, long>(),
            MintedPartitionCounts = new Dictionary<string, int> { ["topic-a"] = 2 }
        };
        var assigned = new List<TopicPartition>
        {
            new("topic-b", new Partition(0)),
            new("topic-b", new Partition(1))
        };

        // act
        void Act() => KafkaEventStreamBroker.ValidateNoPartitionShrink(assigned, resumeState);

        // assert
        Assert.Throws<InvalidEventMessageCursorException>(Act);
    }

    [Fact]
    public void ValidateNoPartitionShrink_Should_NotThrow_When_PartitionsGrew()
    {
        // arrange
        // More live partitions than the cursor minted is a growth, which is honored, not rejected.
        var resumeState = new KafkaResumeState
        {
            Offsets = new Dictionary<TopicPartition, long>(),
            MintedPartitionCounts = new Dictionary<string, int> { ["topic-a"] = 1 }
        };
        var assigned = new List<TopicPartition>
        {
            new("topic-a", new Partition(0)),
            new("topic-a", new Partition(1))
        };

        // act
        var exception = Record.Exception(
            () => KafkaEventStreamBroker.ValidateNoPartitionShrink(assigned, resumeState));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void CreateCursorMap_Should_ReturnNull_When_CursorNotRequiredAndNoResume()
    {
        // arrange

        // act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: false, resumeState: null);

        // assert
        // No output cursor and no inbound resume means the pump tracks nothing per message.
        Assert.Null(map);
    }

    [Fact]
    public void CreateCursorMap_Should_CopyResume_When_CursorNotRequiredButResumeSupplied()
    {
        // arrange
        var resumeState = ResumeStateFor(new TopicPartition("topic-a", new Partition(0)), 7);

        // act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: false, resumeState);

        // assert
        // An inbound resume must survive rebalances even when the output cursor is suppressed, so the
        // progress map is a distinct copy of the resume cursor's offsets.
        Assert.NotSame(resumeState.Offsets, map);
        Assert.Equal(resumeState.Offsets, map);
    }

    [Fact]
    public void CreateCursorMap_Should_ReturnEmptyMap_When_CursorRequiredWithoutResume()
    {
        // arrange

        // act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: true, resumeState: null);

        // assert
        Assert.NotNull(map);
        Assert.Empty(map);
    }

    [Fact]
    public void CreateCursorMap_Should_CopyResume_When_CursorRequiredWithResume()
    {
        // arrange
        var resumeState = ResumeStateFor(new TopicPartition("topic-a", new Partition(0)), 7);

        // act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: true, resumeState);

        // assert
        Assert.NotSame(resumeState.Offsets, map);
        Assert.Equal(resumeState.Offsets, map);
    }

    private static Dictionary<TopicPartition, Offset> Resolve(
        IPartitionWatermarkSource watermarks,
        List<TopicPartition> assigned,
        KafkaResumeState? resumeState,
        Dictionary<TopicPartition, long>? cursorMap,
        AutoOffsetReset autoOffsetReset)
        => KafkaEventStreamBroker.ResolveStartOffsets(
                watermarks,
                assigned,
                resumeState,
                cursorMap,
                autoOffsetReset,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1))
            .ToDictionary(x => x.TopicPartition, x => x.Offset);

    private static KafkaResumeState ResumeStateFor(TopicPartition partition, long offset)
        => new()
        {
            Offsets = new Dictionary<TopicPartition, long> { [partition] = offset },
            MintedPartitionCounts = new Dictionary<string, int> { [partition.Topic] = 1 }
        };

    private sealed class ThrowingWatermarkSource : IPartitionWatermarkSource
    {
        public WatermarkOffsets Query(TopicPartition partition, TimeSpan timeout)
            => throw new KafkaException(ErrorCode.Local_TimedOut);
    }

    private sealed class StubWatermarkSource(WatermarkOffsets watermark) : IPartitionWatermarkSource
    {
        public WatermarkOffsets Query(TopicPartition partition, TimeSpan timeout) => watermark;
    }
}
