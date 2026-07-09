using System.Text;
using System.Threading.Channels;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate.Fusion.Subscriptions.Redis;

public sealed class RedisEventStreamBrokerTests(RedisFixture fixture)
    : IClassFixture<RedisFixture>
{
    [Fact]
    public async Task Subscribe_Should_DeliverPublishedEvent_When_RedisBrokerPublishes()
    {
        // arrange
        var channel = fixture.NextChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var ready = Channel.CreateUnbounded<bool>();
        var services = CreateServices(ready);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("redis");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channel],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await ready.Reader.ReadAsync(cts.Token);

        // act
        await fixture.PublishAsync(channel, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.Equal(0, message.Cursor.Length);
    }

    [Fact]
    public async Task Subscribe_Should_MergeAllChannelsIntoOneStream_When_MultipleTopicsSubscribed()
    {
        // arrange
        var channelA = fixture.NextChannel();
        var channelB = fixture.NextChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var ready = Channel.CreateUnbounded<bool>();
        var services = CreateServices(ready);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("redis");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channelA, channelB],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var events = ReadBodiesAsync(enumerator, count: 2, cts.Token);
        await WaitForReadyAsync(ready.Reader, count: 2, cts.Token);

        // act
        await fixture.PublishAsync(channelA, """{"id":1}""", cts.Token);
        await fixture.PublishAsync(channelB, """{"id":2}""", cts.Token);

        // assert
        Assert.Equal(["""{"id":1}""", """{"id":2}"""], (await events).Order());
    }

    [Fact]
    public async Task Subscribe_Should_DeliverEveryEventToEachSubscriber_When_TwoConcurrentSubscribers()
    {
        // arrange
        var channel = fixture.NextChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var ready = Channel.CreateUnbounded<bool>();
        var services = CreateServices(ready);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerA = factory.Create("redis");
        await using var brokerB = factory.Create("redis");

        await using var enumeratorA = brokerA
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channel],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        await using var enumeratorB = brokerB
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channel],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var nextA = enumeratorA.MoveNextAsync().AsTask();
        var nextB = enumeratorB.MoveNextAsync().AsTask();
        await WaitForReadyAsync(ready.Reader, count: 2, cts.Token);

        // act
        await fixture.PublishAsync(channel, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await nextA);
        using var messageA = enumeratorA.Current;
        Assert.True(await nextB);
        using var messageB = enumeratorB.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(messageA.Body));
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(messageB.Body));
    }

    [Fact]
    public async Task Subscribe_Should_ThrowInvalidCursor_When_CursorSupplied()
    {
        // arrange
        var channel = fixture.NextChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var services = CreateServices();
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("redis");

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channel],
                cursor: "anything",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_CloseConsumerCleanly_When_CancellationRequested()
    {
        // arrange
        var channel = fixture.NextChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var ready = Channel.CreateUnbounded<bool>();
        var services = CreateServices(ready);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("redis");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channel],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync().AsTask();
        await ready.Reader.ReadAsync(cts.Token);

        // act
        await cts.CancelAsync();
        await broker.DisposeAsync();

        // assert
        Assert.False(await pending);
        Assert.Throws<ObjectDisposedException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [channel],
                cursor: null,
                CancellationToken.None));
    }

    [Fact]
    public async Task DisposeAsync_Should_NotDisposeConnectionMultiplexer_When_CallerSupplied()
    {
        // arrange
        await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var services = new ServiceCollection();
        services.AddRedisEventStreamBroker(
            "redis",
            o => o.ConnectionMultiplexer = multiplexer);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("redis");

        // act
        await broker.DisposeAsync();

        // assert
        Assert.True(multiplexer.IsConnected);
        await multiplexer.GetDatabase().PingAsync();
    }

    private ServiceCollection CreateServices(Channel<bool>? ready = null)
    {
        var services = new ServiceCollection();
        services.AddRedisEventStreamBroker(
            "redis",
            o =>
            {
                o.Configuration = fixture.ConnectionString;
                o.OnReceiverReady = ready is null
                    ? null
                    : () => ready.Writer.TryWrite(true);
            });
        return services;
    }

    private static async Task WaitForReadyAsync(
        ChannelReader<bool> ready,
        int count,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            await ready.ReadAsync(cancellationToken);
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
            Assert.Equal(0, message.Cursor.Length);
        }

        return [.. bodies];
    }

    private sealed class EmptySubscriptionFieldContext : ISubscriptionFieldContext
    {
        public static readonly EmptySubscriptionFieldContext Instance = new();

        private EmptySubscriptionFieldContext()
        {
        }

        public IReadOnlyDictionary<string, IValueNode> Arguments { get; } =
            new Dictionary<string, IValueNode>();

        public bool RequiresCursor => true;
    }
}
