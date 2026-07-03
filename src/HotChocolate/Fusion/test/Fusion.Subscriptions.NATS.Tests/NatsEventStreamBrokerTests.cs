using System.Text;
using System.Threading.Channels;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Squadron;

namespace HotChocolate.Fusion.Subscriptions.NATS;

public sealed class NatsEventStreamBrokerTests : IClassFixture<NatsResource>
{
    private readonly NatsResource _natsResource;

    public NatsEventStreamBrokerTests(NatsResource natsResource)
    {
        _natsResource = natsResource;
    }

    [Fact]
    public async Task Subscribe_Should_DeliverPublishedMessage_When_SingleSubjectRoundTrips()
    {
        // arrange
        var subject = CreateSubject();
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = _natsResource.NatsConnectionString;
            o.CreateMessageChannel =
                () => throw new InvalidOperationException(
                    "A channel should not be created for one subject.");
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using var enumerator = broker
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [subject], cursor: null, cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await Task.Delay(250, cts.Token);

        // act
        await using var pubConn = new NatsConnection(
            new NatsOpts { Url = _natsResource.NatsConnectionString });
        await pubConn.PublishAsync(subject, """{"id":1}"""u8.ToArray(), cancellationToken: cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.Equal(0, message.Cursor.Length);
    }

    [Fact]
    public async Task Subscribe_Should_FanInPublishedMessages_When_MultipleSubjectsRoundTrip()
    {
        // arrange
        var subjectA = CreateSubject();
        var subjectB = CreateSubject();
        var channelCreated = false;
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = _natsResource.NatsConnectionString;
            o.CreateMessageChannel = () =>
            {
                channelCreated = true;
                return NatsEventStreamOptions.CreateBoundedMessageChannel(
                    capacity: 1,
                    BoundedChannelFullMode.Wait);
            };
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using var enumerator = broker
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [subjectA, subjectB], cursor: null, cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var first = enumerator.MoveNextAsync().AsTask();
        await Task.Delay(250, cts.Token);

        // act
        await using var pubConn = new NatsConnection(
            new NatsOpts { Url = _natsResource.NatsConnectionString });
        await pubConn.PublishAsync(subjectA, """{"id":1}"""u8.ToArray(), cancellationToken: cts.Token);
        await pubConn.PublishAsync(subjectB, """{"id":2}"""u8.ToArray(), cancellationToken: cts.Token);

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
        Assert.True(channelCreated);
    }

    [Fact]
    public async Task Subscribe_Should_Throw_When_CoreNatsReceivesCursor()
    {
        // arrange
        var subject = CreateSubject();
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
            o.Url = _natsResource.NatsConnectionString);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [subject],
                cursor: "MQ==",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_ThrowFixedCursorError_When_JetStreamCursorIsInvalid()
    {
        // arrange
        var subject = CreateSubject();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = _natsResource.NatsConnectionString;
            o.JetStream = new NatsJetStreamOptions
            {
                Stream = stream
            };
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [subject],
                cursor: "not-base64",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_ResumeFromCursor_When_JetStreamCursorProvided()
    {
        // arrange
        await using var fixture = await JetStreamNatsFixture.StartAsync();
        var subjectA = CreateSubject();
        var subjectB = CreateSubject();
        var stream = "S" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await CreateStreamAsync(fixture.Url, stream, [subjectA, subjectB], cts.Token);
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = fixture.Url;
            o.JetStream = new NatsJetStreamOptions
            {
                Stream = stream
            };
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        string capturedCursor;

        // act
        // phase 1: subscribe fresh, read the first event and capture its cursor.
        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [subjectA, subjectB],
                    cursor: null,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var first = enumerator.MoveNextAsync().AsTask();
            await WaitForConsumerAsync(fixture.Url, stream, expectedCount: 1, cts.Token);
            await PublishJetStreamAsync(fixture.Url, subjectA, """{"id":1}"""u8.ToArray(), cts.Token);

            Assert.True(await first);
            using var firstMessage = enumerator.Current;
            Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(firstMessage.Body));
            capturedCursor = Encoding.UTF8.GetString(firstMessage.Cursor);
        }

        // publish a gap event while no subscriber is connected.
        await PublishJetStreamAsync(fixture.Url, subjectB, """{"id":2}"""u8.ToArray(), cts.Token);

        // phase 2: resume from the captured cursor and recover the missed gap event.
        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(
                    EmptySubscriptionFieldContext.Instance,
                    [subjectA, subjectB],
                    cursor: capturedCursor,
                    cts.Token)
                .GetAsyncEnumerator(cts.Token);

            // assert
            Assert.True(await enumerator.MoveNextAsync());
            using var resumed = enumerator.Current;
            Assert.Equal("""{"id":2}""", Encoding.UTF8.GetString(resumed.Body));
        }
    }

    [Fact]
    public async Task Subscribe_Should_OnlyDeliverNewEvents_When_FreshJetStreamSubscribe()
    {
        // arrange
        await using var fixture = await JetStreamNatsFixture.StartAsync();
        var subject = CreateSubject();
        var stream = "S" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await CreateStreamAsync(fixture.Url, stream, [subject], cts.Token);

        // a historic event is retained before the subscription is established.
        await PublishJetStreamAsync(fixture.Url, subject, """{"id":1}"""u8.ToArray(), cts.Token);

        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = fixture.Url;
            o.JetStream = new NatsJetStreamOptions
            {
                Stream = stream
            };
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);

        // act
        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [subject],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await WaitForConsumerAsync(fixture.Url, stream, expectedCount: 1, cts.Token);
        await PublishJetStreamAsync(fixture.Url, subject, """{"id":2}"""u8.ToArray(), cts.Token);

        // assert
        // the fresh subscription skips the retained history and only sees the new event.
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":2}""", Encoding.UTF8.GetString(message.Body));
    }

    [Fact]
    public async Task Subscribe_Should_FanOutToAllSubscribers_When_MultipleConcurrentJetStreamSubscriptions()
    {
        // arrange
        await using var fixture = await JetStreamNatsFixture.StartAsync();
        var subject = CreateSubject();
        var stream = "S" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await CreateStreamAsync(fixture.Url, stream, [subject], cts.Token);
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = fixture.Url;
            o.JetStream = new NatsJetStreamOptions
            {
                Stream = stream
            };
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerA = factory.Create(null);
        await using var brokerB = factory.Create(null);

        await using var enumeratorA = brokerA
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [subject],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        await using var enumeratorB = brokerB
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [subject],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var firstA = enumeratorA.MoveNextAsync().AsTask();
        var firstB = enumeratorB.MoveNextAsync().AsTask();
        await WaitForConsumerAsync(fixture.Url, stream, expectedCount: 2, cts.Token);

        // act
        await PublishJetStreamAsync(fixture.Url, subject, """{"id":1}"""u8.ToArray(), cts.Token);
        await PublishJetStreamAsync(fixture.Url, subject, """{"id":2}"""u8.ToArray(), cts.Token);

        // assert
        // both concurrent subscriptions receive the full event stream, not a load-balanced share.
        var bodiesA = await ReadTwoBodiesAsync(enumeratorA, firstA);
        var bodiesB = await ReadTwoBodiesAsync(enumeratorB, firstB);

        Assert.Equal(["""{"id":1}""", """{"id":2}"""], bodiesA);
        Assert.Equal(["""{"id":1}""", """{"id":2}"""], bodiesB);
    }

    private static async Task<List<string>> ReadTwoBodiesAsync(
        IAsyncEnumerator<EventMessage> enumerator,
        Task<bool> first)
    {
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

        return bodies;
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeBrokerConnection_When_BrokerIsDisposed()
    {
        // arrange
        var subject = CreateSubject();
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
            o.Url = _natsResource.NatsConnectionString);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [subject], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var pending = enumerator.MoveNextAsync().AsTask();
            await Task.Delay(250, cts.Token);

            await broker.DisposeAsync();

            // Disposing the broker cancels its active subscriptions.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [subject], cursor: null, cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var next = enumerator.MoveNextAsync().AsTask();
            await Task.Delay(250, cts.Token);

            await using var pubConn = new NatsConnection(
                new NatsOpts { Url = _natsResource.NatsConnectionString });
            await pubConn.PublishAsync(subject, """{"id":4}"""u8.ToArray(), cancellationToken: cts.Token);

            Assert.True(await next);
            using var message = enumerator.Current;
            Assert.Equal("""{"id":4}""", Encoding.UTF8.GetString(message.Body));
        }
    }

    private static async Task CreateStreamAsync(
        string url,
        string stream,
        ICollection<string> subjects,
        CancellationToken cancellationToken)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);
        await js.CreateStreamAsync(
            new StreamConfig
            {
                Name = stream,
                Subjects = subjects
            },
            cancellationToken);
    }

    private static async Task PublishJetStreamAsync(
        string url,
        string subject,
        byte[] body,
        CancellationToken cancellationToken)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);
        await js.PublishAsync(subject, body, cancellationToken: cancellationToken);
    }

    // A fresh JetStream subscription uses DeliverPolicy.New, so it only observes events published
    // after its ephemeral consumer exists on the server. Waiting until the stream reports the
    // expected consumer count makes "subscribe then publish" deterministic instead of racing a
    // fixed delay. The consumer name is server-generated, so we poll by count, not by name.
    private static async Task WaitForConsumerAsync(
        string url,
        string stream,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var info = await js.GetStreamAsync(stream, cancellationToken: cancellationToken);

            if (info.Info.State.ConsumerCount >= expectedCount)
            {
                return;
            }

            await Task.Delay(25, cancellationToken);
        }
    }

    private static string CreateSubject()
        => "fusion." + Guid.NewGuid().ToString("N");

    private sealed class EmptySubscriptionFieldContext : ISubscriptionFieldContext
    {
        public static readonly EmptySubscriptionFieldContext Instance = new();

        private EmptySubscriptionFieldContext()
        {
        }

        public IReadOnlyDictionary<string, IValueNode> Arguments { get; } =
            new Dictionary<string, IValueNode>();
    }

    private sealed class JetStreamNatsFixture : IAsyncDisposable
    {
        private readonly IContainer _container;

        private JetStreamNatsFixture(IContainer container)
        {
            _container = container;
        }

        public string Url => $"nats://localhost:{_container.GetMappedPublicPort(4222)}";

        public static async Task<JetStreamNatsFixture> StartAsync()
        {
            var fixture = new JetStreamNatsFixture(
                new ContainerBuilder("nats:2.10-alpine")
                    .WithPortBinding(4222, assignRandomHostPort: true)
                    .WithCommand("-js")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(4222))
                    .Build());

            await fixture._container.StartAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }
}
