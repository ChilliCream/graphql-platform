using System.Text;
using System.Threading.Channels;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

public sealed class AzureEventHubsEventStreamBrokerTests
    : IClassFixture<AzureEventHubsFixture>
{
    private const string PartitionId = "0";

    private readonly AzureEventHubsFixture _fixture;

    public AzureEventHubsEventStreamBrokerTests(AzureEventHubsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Subscribe_Should_DeliverPublishedEvent_When_AzureBrokerPublishes()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, seeded) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.SingleHub],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await WaitForSeededAsync(seeded.Reader, expectedPartitionCount: 2, cts.Token);

        // act
        await _fixture.PublishAsync(_fixture.SingleHub, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.False(message.Cursor.IsEmpty);
        Assert.True(IsWellFormedBase64(message.Cursor));
    }

    [Fact]
    public async Task Subscribe_Should_MergeAllHubsIntoOneStream_When_MultipleTopicsSubscribed()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, seeded) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.FanInHubA, _fixture.FanInHubB],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var first = enumerator.MoveNextAsync().AsTask();
        await WaitForSeededAsync(seeded.Reader, expectedPartitionCount: 4, cts.Token);

        // act
        await _fixture.PublishAsync(_fixture.FanInHubA, """{"id":1}""", cts.Token);
        await _fixture.PublishAsync(_fixture.FanInHubB, """{"id":2}""", cts.Token);

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
    public async Task Subscribe_Should_DeliverEveryEventToEachSubscriber_When_TwoConcurrentSubscribers()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (servicesA, seededA) = CreateServices(startFromEarliest: true);
        await using var providerA = servicesA.BuildServiceProvider();
        var factoryA = providerA.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerA = factoryA.Create("azure");
        var (servicesB, seededB) = CreateServices(startFromEarliest: true);
        await using var providerB = servicesB.BuildServiceProvider();
        var factoryB = providerB.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerB = factoryB.Create("azure");

        await using var enumeratorA = brokerA
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.FanOutHub],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        await using var enumeratorB = brokerB
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.FanOutHub],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var nextA = enumeratorA.MoveNextAsync().AsTask();
        var nextB = enumeratorB.MoveNextAsync().AsTask();
        await WaitForSeededAsync(seededA.Reader, expectedPartitionCount: 2, cts.Token);
        await WaitForSeededAsync(seededB.Reader, expectedPartitionCount: 2, cts.Token);

        // act
        await _fixture.PublishAsync(_fixture.FanOutHub, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await nextA);
        using var messageA = enumeratorA.Current;
        Assert.True(await nextB);
        using var messageB = enumeratorB.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(messageA.Body));
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(messageB.Body));
    }

    [Fact]
    public async Task Subscribe_Should_ResumeFromStoredPosition_When_SequenceNumberCursorProvided()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await _fixture.PublishToPartitionAsync(
            _fixture.ResumeHub,
            PartitionId,
            """{"id":1}""",
            cts.Token);
        await _fixture.PublishToPartitionAsync(
            _fixture.ResumeHub,
            PartitionId,
            """{"id":2}""",
            cts.Token);
        await _fixture.PublishToPartitionAsync(
            _fixture.ResumeHub,
            PartitionId,
            """{"id":3}""",
            cts.Token);
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        string cursor;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.ResumeHub],
                    cursor: null,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);

            Assert.True(await enumerator.MoveNextAsync());
            using var message = enumerator.Current;
            Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        // act
        string[] bodies;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.ResumeHub],
                    cursor,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            bodies = await ReadBodiesAsync(enumerator, count: 2, cts.Token);
        }

        // assert
        Assert.Equal(["""{"id":2}""", """{"id":3}"""], bodies);
    }

    [Fact]
    public async Task Subscribe_Should_CloseConsumerCleanly_When_CancellationRequested()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.SingleHub],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync().AsTask();

        // act
        await cts.CancelAsync();
        await broker.DisposeAsync();

        // assert
        Assert.False(await pending);
        Assert.Throws<ObjectDisposedException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.SingleHub],
                cursor: null,
                CancellationToken.None));
    }

    [Fact]
    public async Task Subscribe_Should_ResumeMultiHubComposite_When_CursorCapturedAcrossHubs()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await _fixture.PublishToPartitionAsync(
            _fixture.MultiHubResumeHubA,
            PartitionId,
            """{"id":"a1"}""",
            cts.Token);
        await _fixture.PublishToPartitionAsync(
            _fixture.MultiHubResumeHubB,
            PartitionId,
            """{"id":"b1"}""",
            cts.Token);
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        string cursor;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.MultiHubResumeHubA, _fixture.MultiHubResumeHubB],
                    cursor: null,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);

            Assert.True(await enumerator.MoveNextAsync());
            enumerator.Current.Dispose();
            Assert.True(await enumerator.MoveNextAsync());
            using var second = enumerator.Current;
            cursor = Encoding.UTF8.GetString(second.Cursor);
        }

        await _fixture.PublishToPartitionAsync(
            _fixture.MultiHubResumeHubA,
            PartitionId,
            """{"id":"a2"}""",
            cts.Token);
        await _fixture.PublishToPartitionAsync(
            _fixture.MultiHubResumeHubB,
            PartitionId,
            """{"id":"b2"}""",
            cts.Token);

        // act
        string[] bodies;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.MultiHubResumeHubA, _fixture.MultiHubResumeHubB],
                    cursor,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            bodies = await ReadBodiesAsync(enumerator, count: 2, cts.Token);
        }

        // assert
        Assert.Equal(["""{"id":"a2"}""", """{"id":"b2"}"""], bodies.Order());
    }

    [Fact]
    public async Task Subscribe_Should_ThrowInvalidCursor_When_CursorIsMalformedOnMultiHubPath()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.FanInHubA, _fixture.FanInHubB],
                cursor: "not-base64",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_ThrowInvalidCursor_When_CursorIsMalformed()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.SingleHub],
                cursor: "not-base64",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_ResumeSilentPartitionsLosslessly_When_MultiPartitionCursorSeeded()
    {
        // arrange
        // The partitions start empty so their fresh-seed baseline is the next arriving
        // event. Partitions "1" and "2" stay silent through the live phase, so the
        // captured cursor must still resume their later gap events losslessly.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, seeded) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        string cursor;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.MultiPartitionResumeHub],
                    cursor: null,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var live = enumerator.MoveNextAsync().AsTask();
            await WaitForSeededAsync(seeded.Reader, expectedPartitionCount: 3, cts.Token);

            await _fixture.PublishToPartitionAsync(
                _fixture.MultiPartitionResumeHub,
                "0",
                """{"id":"p0-live"}""",
                cts.Token);

            Assert.True(await live);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":"p0-live"}""", Encoding.UTF8.GetString(message.Body));
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        await _fixture.PublishToPartitionAsync(
            _fixture.MultiPartitionResumeHub,
            "0",
            """{"id":"p0-gap"}""",
            cts.Token);
        await _fixture.PublishToPartitionAsync(
            _fixture.MultiPartitionResumeHub,
            "1",
            """{"id":"p1-gap"}""",
            cts.Token);
        await _fixture.PublishToPartitionAsync(
            _fixture.MultiPartitionResumeHub,
            "2",
            """{"id":"p2-gap"}""",
            cts.Token);

        // act
        string[] bodies;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.MultiPartitionResumeHub],
                    cursor,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            bodies = await ReadBodiesAsync(enumerator, count: 3, cts.Token);
        }

        // assert
        Assert.Equal(
            ["""{"id":"p0-gap"}""", """{"id":"p1-gap"}""", """{"id":"p2-gap"}"""],
            bodies.Order());
    }

    [Fact]
    public async Task Subscribe_Should_ThrowInvalidCursor_When_CursorPartitionAbsentFromLiveHub()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");
        var map = new Dictionary<HubPartition, long>
        {
            [new HubPartition(_fixture.SinglePartitionHub, "99")] = 0
        };
        var raw = new byte[AzureEventHubsCompositeCursorFormatter.GetFormattedLength(map)];
        AzureEventHubsCompositeCursorFormatter.Format(map, raw);
        var cursor = Convert.ToBase64String(raw);

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.SinglePartitionHub],
                cursor,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);

        // act
        var exception = await Assert.ThrowsAsync<InvalidEventMessageCursorException>(async () =>
            await enumerator.MoveNextAsync());

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_GateCursor_When_ContextDoesNotRequireCursorFresh()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, _) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.WithoutCursor,
                [_fixture.GatewayHub],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();

        // act
        await _fixture.PublishAsync(_fixture.GatewayHub, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.True(message.Cursor.IsEmpty);
    }

    [Fact]
    public async Task Subscribe_Should_HonorResumeButSuppressCursor_When_ContextDoesNotRequireCursor()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (services, seeded) = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        string cursor;

        await using (var broker = factory.Create("azure"))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [_fixture.SinglePartitionHub],
                    cursor: null,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var first = enumerator.MoveNextAsync().AsTask();
            await WaitForSeededAsync(seeded.Reader, expectedPartitionCount: 1, cts.Token);

            await _fixture.PublishToPartitionAsync(
                _fixture.SinglePartitionHub,
                PartitionId,
                """{"id":1}""",
                cts.Token);

            Assert.True(await first);
            using var message = enumerator.Current;
            cursor = Encoding.UTF8.GetString(message.Cursor);
        }

        await _fixture.PublishToPartitionAsync(
            _fixture.SinglePartitionHub,
            PartitionId,
            """{"id":2}""",
            cts.Token);

        // act
        await using var resumeBroker = factory.Create("azure");
        await using var resumeEnumerator = resumeBroker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.WithoutCursor,
                [_fixture.SinglePartitionHub],
                cursor,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);

        Assert.True(await resumeEnumerator.MoveNextAsync());
        using var resumed = resumeEnumerator.Current;

        // assert
        Assert.Equal("""{"id":2}""", Encoding.UTF8.GetString(resumed.Body));
        Assert.True(resumed.Cursor.IsEmpty);
    }

    private (
        ServiceCollection Services,
        Channel<IReadOnlyList<HubPartition>> Seeded) CreateServices(bool startFromEarliest)
    {
        var services = new ServiceCollection();
        var seeded = Channel.CreateUnbounded<IReadOnlyList<HubPartition>>();
        services.AddAzureEventHubsEventStreamBroker(
            "azure",
            o =>
            {
                o.ConnectionString = _fixture.ConnectionString;
                o.ConsumerGroup = _fixture.ConsumerGroup;
                o.StartFromEarliest = startFromEarliest;
                o.MaximumWaitTime = TimeSpan.FromSeconds(1);
                o.OnPartitionsSeeded = partitions => seeded.Writer.TryWrite(partitions);
                o.ConfigureClientOptions = options =>
                {
                    options.RetryOptions.MaximumRetries = 2;
                    options.RetryOptions.TryTimeout = TimeSpan.FromSeconds(10);
                    return options;
                };
            });
        return (services, seeded);
    }

    private static async Task WaitForSeededAsync(
        ChannelReader<IReadOnlyList<HubPartition>> seeded,
        int expectedPartitionCount,
        CancellationToken cancellationToken)
    {
        var total = new HashSet<HubPartition>();

        while (total.Count < expectedPartitionCount)
        {
            var batch = await seeded.ReadAsync(cancellationToken);

            foreach (var partition in batch)
            {
                total.Add(partition);
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
            if (!await enumerator.MoveNextAsync())
            {
                throw new InvalidOperationException(
                    "The event stream ended before enough messages were read.");
            }

            using var message = enumerator.Current;
            bodies.Add(Encoding.UTF8.GetString(message.Body));
        }

        return [.. bodies];
    }

    private static bool IsWellFormedBase64(ReadOnlySpan<byte> cursor)
        => Convert.TryFromBase64String(
            Encoding.UTF8.GetString(cursor),
            new byte[cursor.Length],
            out _);

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
