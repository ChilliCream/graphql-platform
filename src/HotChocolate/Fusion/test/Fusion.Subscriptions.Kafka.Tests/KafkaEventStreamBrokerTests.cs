using System.Text;
using System.Threading.Channels;
using Confluent.Kafka;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaEventStreamBrokerTests : IClassFixture<KafkaFixture>
{
    private readonly KafkaFixture _fixture;

    public KafkaEventStreamBrokerTests(KafkaFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Subscribe_Should_DeliverPublishedMessage_When_SingleTopicRoundTrips()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);

        await using var enumerator = broker
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

        // act
        await ProduceAsync(topic, """{"id":1}"""u8.ToArray(), cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        // The composite cursor is opaque; assert it is present and a well formed base64 token.
        // Its resume semantics are covered by the round-trip tests below.
        Assert.False(message.Cursor.IsEmpty);
        Assert.True(
            Convert.TryFromBase64String(
                Encoding.UTF8.GetString(message.Cursor),
                new byte[message.Cursor.Length],
                out _));
    }

    [Fact]
    public async Task Subscribe_Should_FanInPublishedMessages_When_MultipleTopicsRoundTrip()
    {
        // arrange
        var topicA = CreateTopic();
        var topicB = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _fixture.CreateTopicAsync(topicA, cts.Token);
        await _fixture.CreateTopicAsync(topicB, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);

        await using var enumerator = broker
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topicA, topicB], cursor: null, cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var first = enumerator.MoveNextAsync().AsTask();
        await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

        // act
        await ProduceAsync(topicA, """{"id":1}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topicB, """{"id":2}"""u8.ToArray(), cts.Token);

        // assert
        var bodies = new List<string>();

        Assert.True(await first);
        using (var firstMessage = enumerator.Current)
        {
            bodies.Add(Encoding.UTF8.GetString(firstMessage.Body));
        }

        Assert.True(await enumerator.MoveNextAsync());
        using (var secondMessage = enumerator.Current)
        {
            bodies.Add(Encoding.UTF8.GetString(secondMessage.Body));
        }

        Assert.Equal(["""{"id":1}""", """{"id":2}"""], bodies.Order());
    }

    [Fact]
    public async Task Subscribe_Should_ThrowFixedCursorError_When_CursorIsInvalid()
    {
        // arrange
        var topic = CreateTopic();
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: "not-base64",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_DeliverEveryEventToEverySubscriber_When_TwoSubscribersShareTopic()
    {
        // arrange
        const int count = 3;
        var topic = CreateTopic();
        var expected = Enumerable.Range(1, count).Select(i => $$"""{"id":{{i}}}""").ToArray();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerA = factory.Create(null);
        await using var brokerB = factory.Create(null);

        await using var enumeratorA = brokerA
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
            .GetAsyncEnumerator(cts.Token);
        await using var enumeratorB = brokerB
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var eventsA = ReadBodiesAsync(enumeratorA, count, cts.Token);
        var eventsB = ReadBodiesAsync(enumeratorB, count, cts.Token);
        await WaitForAssignmentsAsync(assignments.Reader, count: 2, cts.Token);

        // act
        for (var i = 0; i < expected.Length; i++)
        {
            await ProduceAsync(topic, Encoding.UTF8.GetBytes(expected[i]), cts.Token);
        }

        // assert
        Assert.Equal(expected, (await eventsA).Order());
        Assert.Equal(expected, (await eventsB).Order());
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseConsumerCleanly_When_BrokerIsDisposed()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var pending = enumerator.MoveNextAsync().AsTask();
            await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

            await broker.DisposeAsync();
            Assert.False(await pending);
        }

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var next = enumerator.MoveNextAsync().AsTask();
            await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

            // act
            await ProduceAsync(topic, """{"id":4}"""u8.ToArray(), cts.Token);

            // assert
            Assert.True(await next);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":4}""", Encoding.UTF8.GetString(message.Body));
        }
    }

    [Fact]
    public async Task Subscribe_Should_ResumeSilentTopicLosslessly_When_MultiTopicCursorSeeded()
    {
        // arrange
        var topicA = CreateTopic();
        var topicB = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.CreateTopicAsync(topicA, cts.Token);
        await _fixture.CreateTopicAsync(topicB, cts.Token);
        // Pre-existing history so that the beginning differs from the subscribe-time position. If
        // topic B were not seeded at subscribe, a resume would fall back to the beginning and
        // redeliver this history, which is exactly the loss this test guards against.
        await ProduceAsync(topicA, """{"id":"a-old-0"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topicA, """{"id":"a-old-1"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topicB, """{"id":"b-old-0"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topicB, """{"id":"b-old-1"}"""u8.ToArray(), cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        string cursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [topicA, topicB],
                    cursor: null,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var live = enumerator.MoveNextAsync().AsTask();
            // Both partitions must be assigned and seeded before the live event is produced,
            // otherwise topic B could be seeded after its subscribe-time position and lose the gap.
            await WaitForPartitionCountAsync(assignments.Reader, count: 2, cts.Token);

            // A single live event on topic A only; topic B stays silent for this session.
            await ProduceAsync(topicA, """{"id":"a-live"}"""u8.ToArray(), cts.Token);

            Assert.True(await live);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":"a-live"}""", Encoding.UTF8.GetString(message.Body));
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // Gap events published to BOTH topics while disconnected.
        await ProduceAsync(topicA, """{"id":"a-gap"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topicB, """{"id":"b-gap"}"""u8.ToArray(), cts.Token);

        // act
        string[] resumed;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [topicA, topicB],
                    cursor,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            resumed = await ReadBodiesAsync(enumerator, count: 2, cts.Token);
        }

        // assert
        Assert.Equal(["""{"id":"a-gap"}""", """{"id":"b-gap"}"""], resumed.Order());
    }

    [Fact]
    public async Task Subscribe_Should_SkipCursor_When_ContextDoesNotRequireCursor()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        // Without a tracked cursor the start offset is left unset and resolved by AutoOffsetReset.
        // Earliest pins the empty topic's beginning so the event produced after subscribe is read.
        var services = CreateServices(assignments, AutoOffsetReset.Earliest);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.WithoutCursor,
                [topic],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

        // act
        await ProduceAsync(topic, """{"id":1}"""u8.ToArray(), cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        // No output cursor is exposed, so the broker delivers the body without cursor data.
        Assert.True(message.Cursor.IsEmpty);
    }

    [Fact]
    public async Task Subscribe_Should_ResumeAfterCapturedEvent_When_SingleTopicCursorReused()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        string cursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var first = enumerator.MoveNextAsync().AsTask();
            await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

            await ProduceAsync(topic, """{"id":1}"""u8.ToArray(), cts.Token);

            Assert.True(await first);
            using var message = enumerator.Current;
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // A second event published while disconnected.
        await ProduceAsync(topic, """{"id":2}"""u8.ToArray(), cts.Token);

        // act
        string resumedBody;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            Assert.True(await enumerator.MoveNextAsync());
            using var message = enumerator.Current;
            resumedBody = Encoding.UTF8.GetString(message.Body);
        }

        // assert
        Assert.Equal("""{"id":2}""", resumedBody);
    }

    [Fact]
    public async Task Subscribe_Should_ReplayHistory_When_AutoOffsetResetIsEarliest()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        // Pre-existing history produced before the subscribe. With Earliest a fresh cursor-enabled
        // subscribe must replay it from the earliest retained event.
        await ProduceAsync(topic, """{"id":"old-0"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topic, """{"id":"old-1"}"""u8.ToArray(), cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments, AutoOffsetReset.Earliest);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        // act
        var history = new List<string>();
        string cursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);

            Assert.True(await enumerator.MoveNextAsync());
            using (var first = enumerator.Current)
            {
                history.Add(Encoding.UTF8.GetString(first.Body));
            }

            Assert.True(await enumerator.MoveNextAsync());
            using var second = enumerator.Current;
            history.Add(Encoding.UTF8.GetString(second.Body));
            cursor = Encoding.UTF8.GetString(second.Cursor);
        }

        // A gap event published while disconnected; the resume must continue past the replayed
        // history rather than replay it a second time.
        await ProduceAsync(topic, """{"id":"gap"}"""u8.ToArray(), cts.Token);

        string[] resumed;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            resumed = await ReadBodiesAsync(enumerator, count: 1, cts.Token);
        }

        // assert
        Assert.Equal(["""{"id":"old-0"}""", """{"id":"old-1"}"""], history);
        Assert.Equal(["""{"id":"gap"}"""], resumed);
    }

    [Fact]
    public async Task Subscribe_Should_HonorResumeButSuppressCursor_When_CursorNotRequired()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        // A prior cursor-tracking session captures a resume cursor after the first event.
        string cursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var first = enumerator.MoveNextAsync().AsTask();
            await WaitForPartitionCountAsync(assignments.Reader, count: 1, cts.Token);

            await ProduceAsync(topic, """{"id":1}"""u8.ToArray(), cts.Token);

            Assert.True(await first);
            using var message = enumerator.Current;
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // A second event published while disconnected; the resume must skip the captured event.
        await ProduceAsync(topic, """{"id":2}"""u8.ToArray(), cts.Token);

        // act
        await using var resumeBroker = factory.Create(null);
        await using var resumeEnumerator = resumeBroker
            .SubscribeAsync(EmptySubscriptionFieldContext.WithoutCursor, [topic], cursor, cts.Token)
            .GetAsyncEnumerator(cts.Token);

        Assert.True(await resumeEnumerator.MoveNextAsync());
        using var resumed = resumeEnumerator.Current;

        // assert
        // The inbound resume seek is honored even though the operation does not expose a cursor, so
        // only the post-cursor event is delivered, and its own output cursor is suppressed.
        Assert.Equal("""{"id":2}""", Encoding.UTF8.GetString(resumed.Body));
        Assert.True(resumed.Cursor.IsEmpty);
    }

    [Fact]
    public async Task Subscribe_Should_PreserveSilentPartition_When_ResumedAcrossMultipleHops()
    {
        // arrange
        var topicA = CreateTopic();
        var topicB = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await _fixture.CreateTopicAsync(topicA, cts.Token);
        await _fixture.CreateTopicAsync(topicB, cts.Token);
        // Pre-existing history on topic B so its subscribe-time position differs from the beginning.
        // If that position were lost across a hop, a resume would fall back to the beginning and
        // redeliver this history instead of the single gap event.
        await ProduceAsync(topicB, """{"id":"b-old-0"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(topicB, """{"id":"b-old-1"}"""u8.ToArray(), cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        // first hop: a live event on topic A only; topic B stays silent but is seeded.
        string firstCursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topicA, topicB], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var live = enumerator.MoveNextAsync().AsTask();
            await WaitForPartitionCountAsync(assignments.Reader, count: 2, cts.Token);

            await ProduceAsync(topicA, """{"id":"a-live-1"}"""u8.ToArray(), cts.Token);

            Assert.True(await live);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":"a-live-1"}""", Encoding.UTF8.GetString(message.Body));
            firstCursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // second hop: resume, topic B still silent, another live event on topic A carries B's
        // preserved position forward into the next cursor.
        string secondCursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topicA, topicB], firstCursor, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var live = enumerator.MoveNextAsync().AsTask();
            await WaitForPartitionCountAsync(assignments.Reader, count: 2, cts.Token);

            await ProduceAsync(topicA, """{"id":"a-live-2"}"""u8.ToArray(), cts.Token);

            Assert.True(await live);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":"a-live-2"}""", Encoding.UTF8.GetString(message.Body));
            secondCursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // A topic B gap event published before the third connect.
        await ProduceAsync(topicB, """{"id":"b-gap"}"""u8.ToArray(), cts.Token);

        // act: the third hop recovers exactly the topic B gap event.
        string[] resumed;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topicA, topicB], secondCursor, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            resumed = await ReadBodiesAsync(enumerator, count: 1, cts.Token);
        }

        // assert
        Assert.Equal(["""{"id":"b-gap"}"""], resumed);
    }

    [Fact]
    public async Task Subscribe_Should_ResumeSilentPartitionsLosslessly_When_MultiPartitionCursorSeeded()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.CreateTopicAsync(topic, cts.Token, partitions: 3);
        var partition0 = new TopicPartition(topic, new Partition(0));
        var partition1 = new TopicPartition(topic, new Partition(1));
        var partition2 = new TopicPartition(topic, new Partition(2));
        // Pre-existing history on every partition so that the beginning differs from the
        // subscribe-time position. If a partition's baseline were not captured in the cursor, a
        // resume would fall back to the beginning and redeliver this history, which is exactly the
        // loss this test guards against.
        await ProduceAsync(partition0, """{"id":"p0-old"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(partition1, """{"id":"p1-old"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(partition2, """{"id":"p2-old"}"""u8.ToArray(), cts.Token);
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        string cursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var live = enumerator.MoveNextAsync().AsTask();
            // All three partitions must be assigned and seeded before the live event is produced,
            // otherwise a partition could be seeded after its subscribe-time position and lose the
            // gap.
            await WaitForPartitionCountAsync(assignments.Reader, count: 3, cts.Token);

            // A single live event on partition 0 only; partitions 1 and 2 stay silent this session.
            await ProduceAsync(partition0, """{"id":"p0-live"}"""u8.ToArray(), cts.Token);

            Assert.True(await live);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":"p0-live"}""", Encoding.UTF8.GetString(message.Body));
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // Gap events published to ALL three partitions while disconnected.
        await ProduceAsync(partition0, """{"id":"p0-gap"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(partition1, """{"id":"p1-gap"}"""u8.ToArray(), cts.Token);
        await ProduceAsync(partition2, """{"id":"p2-gap"}"""u8.ToArray(), cts.Token);

        // act
        string[] resumed;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            resumed = await ReadBodiesAsync(enumerator, count: 3, cts.Token);
        }

        // assert
        Assert.Equal(
            ["""{"id":"p0-gap"}""", """{"id":"p1-gap"}""", """{"id":"p2-gap"}"""],
            resumed.Order());
    }

    [Fact]
    public async Task Subscribe_Should_DeliverGrownPartitionFromBeginning_When_ResumedAfterPartitionIncrease()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.CreateTopicAsync(topic, cts.Token);
        var partition0 = new TopicPartition(topic, new Partition(0));
        var partition1 = new TopicPartition(topic, new Partition(1));
        var assignments = Channel.CreateUnbounded<IReadOnlyList<TopicPartition>>();
        var services = CreateServices(assignments);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        string cursor;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var live = enumerator.MoveNextAsync().AsTask();
            // The single partition must be assigned and seeded before the live event is produced, so
            // the captured cursor sits exactly after it.
            await WaitForPartitionCountAsync(assignments.Reader, count: 1, cts.Token);

            await ProduceAsync(partition0, """{"id":"p0-live"}"""u8.ToArray(), cts.Token);

            Assert.True(await live);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":"p0-live"}""", Encoding.UTF8.GetString(message.Body));
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // Grow the topic and publish a gap event to the brand new partition while disconnected.
        await _fixture.IncreasePartitionsAsync(topic, newTotalCount: 2, cts.Token);
        await ProduceAsync(partition1, """{"id":"p1-gap"}"""u8.ToArray(), cts.Token);

        // act
        string[] resumed;

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cursor, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var gap = enumerator.MoveNextAsync().AsTask();
            // On resume the topic has grown to two partitions, so the assignment barrier must cover
            // both before the grown partition can be read from its beginning.
            await WaitForPartitionCountAsync(assignments.Reader, count: 2, cts.Token);

            Assert.True(await gap);
            using var message = enumerator.Current;
            resumed = [Encoding.UTF8.GetString(message.Body)];
        }

        // assert
        // The grown partition is seeded from its beginning and delivers its gap event, while the
        // tracked partition 0 resumes past the captured live event and redelivers nothing.
        Assert.Equal(["""{"id":"p1-gap"}"""], resumed);
    }

    private ServiceCollection CreateServices(
        Channel<IReadOnlyList<TopicPartition>> assignments,
        AutoOffsetReset autoOffsetReset = AutoOffsetReset.Latest)
    {
        var services = new ServiceCollection();
        services.AddKafkaEventStreamBroker(
            configure: o =>
            {
                o.BootstrapServers = _fixture.BootstrapServers;
                o.AutoOffsetReset = autoOffsetReset;
                o.OnPartitionsAssigned = partitions => assignments.Writer.TryWrite(partitions);
            });
        return services;
    }

    private async Task ProduceAsync(
        string topic,
        byte[] body,
        CancellationToken cancellationToken)
    {
        using var producer = new ProducerBuilder<Null, byte[]>(
            new ProducerConfig { BootstrapServers = _fixture.BootstrapServers })
            .Build();
        await producer.ProduceAsync(
            topic,
            new Message<Null, byte[]> { Value = body },
            cancellationToken);
        producer.Flush(cancellationToken);
    }

    private async Task ProduceAsync(
        TopicPartition topicPartition,
        byte[] body,
        CancellationToken cancellationToken)
    {
        using var producer = new ProducerBuilder<Null, byte[]>(
            new ProducerConfig { BootstrapServers = _fixture.BootstrapServers })
            .Build();
        await producer.ProduceAsync(
            topicPartition,
            new Message<Null, byte[]> { Value = body },
            cancellationToken);
        producer.Flush(cancellationToken);
    }

    private static async Task WaitForAssignmentsAsync(
        ChannelReader<IReadOnlyList<TopicPartition>> assignments,
        int count,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            var partitions = await assignments.ReadAsync(cancellationToken);
            Assert.NotEmpty(partitions);
        }
    }

    private static async Task WaitForPartitionCountAsync(
        ChannelReader<IReadOnlyList<TopicPartition>> assignments,
        int count,
        CancellationToken cancellationToken)
    {
        // Kafka may assign the subscribed partitions in one rebalance or across several, so the
        // seeded barrier accumulates distinct assignments until every expected partition is covered.
        var assigned = new HashSet<TopicPartition>();

        while (assigned.Count < count)
        {
            var partitions = await assignments.ReadAsync(cancellationToken);

            foreach (var partition in partitions)
            {
                assigned.Add(partition);
            }
        }
    }

    private static async Task<string[]> ReadBodiesAsync(
        IAsyncEnumerator<EventMessage> enumerator,
        int count,
        CancellationToken cancellationToken)
    {
        var bodies = new List<string>();

        for (var i = 0; i < count; i++)
        {
            Assert.True(await enumerator.MoveNextAsync());
            using var message = enumerator.Current;
            bodies.Add(Encoding.UTF8.GetString(message.Body));
        }

        return [.. bodies];
    }

    private static string CreateTopic()
        => "fusion-" + Guid.NewGuid().ToString("N");

    private sealed class EmptySubscriptionFieldContext : ISubscriptionFieldContext
    {
        public static readonly EmptySubscriptionFieldContext Instance = new(requiresCursor: true);

        public static readonly EmptySubscriptionFieldContext WithoutCursor = new(requiresCursor: false);

        private EmptySubscriptionFieldContext(bool requiresCursor)
        {
            RequiresCursor = requiresCursor;
        }

        public IReadOnlyDictionary<string, IValueNode> Arguments { get; } =
            new Dictionary<string, IValueNode>();

        public bool RequiresCursor { get; }
    }
}
