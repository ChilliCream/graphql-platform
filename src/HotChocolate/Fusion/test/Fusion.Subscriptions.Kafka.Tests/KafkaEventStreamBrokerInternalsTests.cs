using System.Text;
using Confluent.Kafka;

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
        // A delivered write commits the advance to the next offset (5 -> 6).
        Assert.Equal(6L, cursorMap[partition]);
        Assert.True(channel.Reader.TryRead(out var delivered));
        using (delivered)
        {
            Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(delivered!.Body));
            // The delivered message's own cursor resumes past itself (offset 5 -> next offset 6).
            var cursor = KafkaCompositeCursorFormatter.Parse(
                Encoding.UTF8.GetString(delivered.Cursor), ["topic-a"]);
            Assert.Equal(6L, cursor[partition]);
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
    public void TryResolveTrackedOffset_Should_ReturnTrackedOffsetWithoutWatermark_When_PartitionAlreadyTracked()
    {
        // arrange
        // A partition already tracked in the cursor map (from an earlier assignment or a delivered
        // message) must resume from its tracked next offset. TryResolveTrackedOffset takes no
        // consumer, so it cannot query the watermark: resolving from the map alone is the seed-once
        // guarantee that stops a rebalance from re-seeding a tracked partition forward and skipping
        // events.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var cursorMap = new Dictionary<TopicPartition, long> { [partition] = 42 };

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap: null, cursorMap, AutoOffsetReset.Latest, out var offset);

        // assert
        Assert.True(resolved);
        Assert.Equal(new Offset(42), offset);
    }

    [Fact]
    public void TryResolveTrackedOffset_Should_PreferCursorMap_When_BothMapsContainPartition()
    {
        // arrange
        // The cursor map holds the position reached this session and takes precedence over the
        // inbound resume cursor, so a rebalance resumes from live progress rather than rewinding.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeMap = new Dictionary<TopicPartition, long> { [partition] = 7 };
        var cursorMap = new Dictionary<TopicPartition, long> { [partition] = 42 };

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap, cursorMap, AutoOffsetReset.Latest, out var offset);

        // assert
        Assert.True(resolved);
        Assert.Equal(new Offset(42), offset);
    }

    [Fact]
    public void TryResolveTrackedOffset_Should_HonorResumeMap_When_CursorMapNull()
    {
        // arrange
        // An inbound-only resume (no output cursor tracked) seeks to the stored next offset directly.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeMap = new Dictionary<TopicPartition, long> { [partition] = 7 };

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap, cursorMap: null, AutoOffsetReset.Latest, out var offset);

        // assert
        Assert.True(resolved);
        Assert.Equal(new Offset(7), offset);
    }

    [Fact]
    public void TryResolveTrackedOffset_Should_StartAtLiveEnd_When_PartitionAbsentFromResumeUnderLatest()
    {
        // arrange
        // A (topic, partition) absent from the resume cursor was never baselined (a seed failure) or
        // is genuinely new. Under Latest the reset mode starts it at the live end so a resume does
        // not replay history, matching SeedFreshPartition's Latest-to-High seeding.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeMap = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-b", new Partition(0))] = 7
        };

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap, cursorMap: null, AutoOffsetReset.Latest, out var offset);

        // assert
        Assert.True(resolved);
        Assert.Equal(Offset.End, offset);
    }

    [Fact]
    public void TryResolveTrackedOffset_Should_StartAtBeginning_When_PartitionAbsentFromResumeUnderEarliest()
    {
        // arrange
        // Under Earliest an absent partition replays from the beginning, the lossless choice for a
        // partition that was never baselined or is new relative to the resume cursor.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeMap = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-b", new Partition(0))] = 7
        };

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap, cursorMap: null, AutoOffsetReset.Earliest, out var offset);

        // assert
        Assert.True(resolved);
        Assert.Equal(Offset.Beginning, offset);
    }

    [Fact]
    public void TryResolveTrackedOffset_Should_NotResolve_When_CursorMapMissesPartitionAndNoResume()
    {
        // arrange
        // The partition is not yet in the cursor map and no resume cursor exists, so the caller must
        // fall through to fresh watermark seeding rather than resolve a tracked offset.
        var partition = new TopicPartition("topic-a", new Partition(0));
        var cursorMap = new Dictionary<TopicPartition, long>
        {
            [new TopicPartition("topic-b", new Partition(0))] = 42
        };

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap: null, cursorMap, AutoOffsetReset.Latest, out var offset);

        // assert
        Assert.False(resolved);
        Assert.Equal(Offset.Unset, offset);
    }

    [Fact]
    public void TryResolveTrackedOffset_Should_NotResolve_When_BothMapsNull()
    {
        // arrange
        // A fresh non-resumable subscription tracks nothing, so resolution falls through to the
        // configured AutoOffsetReset at the transport.
        var partition = new TopicPartition("topic-a", new Partition(0));

        // act
        var resolved = KafkaEventStreamBroker.TryResolveTrackedOffset(
            partition, resumeMap: null, cursorMap: null, AutoOffsetReset.Latest, out var offset);

        // assert
        Assert.False(resolved);
        Assert.Equal(Offset.Unset, offset);
    }

    [Fact]
    public void CreateCursorMap_Should_ReturnNull_When_CursorNotRequiredAndNoResume()
    {
        // arrange & act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: false, resumeMap: null);

        // assert
        // No output cursor and no inbound resume means the pump tracks nothing per message.
        Assert.Null(map);
    }

    [Fact]
    public void CreateCursorMap_Should_CopyResume_When_CursorNotRequiredButResumeSupplied()
    {
        // arrange
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeMap = new Dictionary<TopicPartition, long> { [partition] = 7 };

        // act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: false, resumeMap);

        // assert
        // An inbound resume must survive rebalances even when the output cursor is suppressed, so the
        // progress map is a distinct copy of the resume cursor.
        Assert.NotSame(resumeMap, map);
        Assert.Equal(resumeMap, map);
    }

    [Fact]
    public void CreateCursorMap_Should_ReturnEmptyMap_When_CursorRequiredWithoutResume()
    {
        // arrange & act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: true, resumeMap: null);

        // assert
        Assert.NotNull(map);
        Assert.Empty(map);
    }

    [Fact]
    public void CreateCursorMap_Should_CopyResume_When_CursorRequiredWithResume()
    {
        // arrange
        var partition = new TopicPartition("topic-a", new Partition(0));
        var resumeMap = new Dictionary<TopicPartition, long> { [partition] = 7 };

        // act
        var map = KafkaEventStreamBroker.CreateCursorMap(requiresCursor: true, resumeMap);

        // assert
        Assert.NotSame(resumeMap, map);
        Assert.Equal(resumeMap, map);
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
        // The progress advance still fires (5 -> 6) so a later rebalance resumes past this message.
        Assert.Equal(6L, cursorMap[partition]);
        Assert.True(channel.Reader.TryRead(out var delivered));
        using (delivered)
        {
            Assert.True(delivered!.Cursor.IsEmpty);
        }
    }
}
