using System.Text;
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
        var services = CreateServices(startFromEarliest: true);
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

        // act
        await _fixture.PublishAsync(_fixture.SingleHub, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.Equal(0, message.Cursor.Length);
    }

    [Fact]
    public async Task Subscribe_Should_MergeAllHubsIntoOneStream_When_MultipleTopicsSubscribed()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var services = CreateServices(startFromEarliest: true);
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
        var services = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerA = factory.Create("azure");
        await using var brokerB = factory.Create("azure");

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
        var cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{PartitionId}:0"));
        var services = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.ResumeHub],
                cursor,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);

        // act
        var bodies = await ReadBodiesAsync(enumerator, count: 2, cts.Token);

        // assert
        Assert.Equal(["""{"id":2}""", """{"id":3}"""], bodies);
    }

    [Fact]
    public async Task Subscribe_Should_CloseConsumerCleanly_When_CancellationRequested()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var services = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.CancellationHub],
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
                [_fixture.CancellationHub],
                cursor: null,
                CancellationToken.None));
    }

    [Fact]
    public async Task Subscribe_Should_ThrowInvalidCursor_When_CursorSuppliedOnFanInPath()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var services = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");
        var cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes("0:0"));

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.FanInHubA, _fixture.FanInHubB],
                cursor,
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_ThrowInvalidCursor_When_CursorIsMalformed()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var services = CreateServices(startFromEarliest: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("azure");

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [_fixture.InvalidCursorHub],
                cursor: "not-base64",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    private ServiceCollection CreateServices(bool startFromEarliest)
    {
        var services = new ServiceCollection();
        services.AddAzureEventHubsEventStreamBroker(
            "azure",
            o =>
            {
                o.ConnectionString = _fixture.ConnectionString;
                o.ConsumerGroup = _fixture.ConsumerGroup;
                o.StartFromEarliest = startFromEarliest;
                o.MaximumWaitTime = TimeSpan.FromSeconds(1);
                o.ConfigureClientOptions = options =>
                {
                    options.RetryOptions.MaximumRetries = 2;
                    options.RetryOptions.TryTimeout = TimeSpan.FromSeconds(10);
                    return options;
                };
            });
        return services;
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
            Assert.True(message.Cursor.Length > 0);
            _ = DecodeCursor(message.Cursor);
        }

        return [.. bodies];
    }

    private static (string PartitionId, long SequenceNumber) DecodeCursor(ReadOnlySpan<byte> cursor)
    {
        var token = Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(cursor)));
        var separator = token.LastIndexOf(':');
        Assert.True(separator > 0);
        Assert.True(long.TryParse(token[(separator + 1)..], out var sequenceNumber));

        return (token[..separator], sequenceNumber);
    }

    private sealed class EmptySubscriptionFieldContext : ISubscriptionFieldContext
    {
        public static readonly EmptySubscriptionFieldContext Instance = new();

        private EmptySubscriptionFieldContext()
        {
        }

        public IReadOnlyDictionary<string, IValueNode> Arguments { get; } =
            new Dictionary<string, IValueNode>();
    }
}
