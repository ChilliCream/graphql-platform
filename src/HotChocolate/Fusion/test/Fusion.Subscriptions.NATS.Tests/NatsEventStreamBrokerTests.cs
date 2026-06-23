using System.Text;
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
            o.Url = _natsResource.NatsConnectionString);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using var enumerator = broker
            .Subscribe(EmptySubscriptionFieldContext.Instance, [subject], cts.Token)
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
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
            o.Url = _natsResource.NatsConnectionString);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using var enumerator = broker
            .Subscribe(EmptySubscriptionFieldContext.Instance, [subjectA, subjectB], cts.Token)
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
    }

    [Fact]
    public async Task Subscribe_Should_ResumeDurableConsumer_When_JetStreamCursorIsAcked()
    {
        // arrange
        await using var fixture = await JetStreamNatsFixture.StartAsync();
        var subjectA = CreateSubject();
        var subjectB = CreateSubject();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await CreateStreamAsync(fixture.Url, stream, [subjectA, subjectB], cts.Token);
        var services = new ServiceCollection();
        services.AddNatsEventStreamBroker(configure: o =>
        {
            o.Url = fixture.Url;
            o.JetStream = new NatsJetStreamOptions
            {
                Stream = stream,
                DurableConsumer = durable
            };
        });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .Subscribe(EmptySubscriptionFieldContext.Instance, [subjectA, subjectB], cts.Token)
                .GetAsyncEnumerator(cts.Token);
            await PublishJetStreamAsync(fixture.Url, subjectA, """{"id":1}"""u8.ToArray(), cts.Token);
            await PublishJetStreamAsync(fixture.Url, subjectB, """{"id":2}"""u8.ToArray(), cts.Token);

            Assert.True(await enumerator.MoveNextAsync());
            using var first = enumerator.Current;
            Assert.True(first.Cursor.Length > 0);
            var firstCursor = ulong.Parse(Encoding.UTF8.GetString(first.Cursor));

            Assert.True(await enumerator.MoveNextAsync());
            using var second = enumerator.Current;
            Assert.True(second.Cursor.Length > 0);
            var secondCursor = ulong.Parse(Encoding.UTF8.GetString(second.Cursor));

            Assert.True(secondCursor > firstCursor);
        }

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .Subscribe(EmptySubscriptionFieldContext.Instance, [subjectA, subjectB], cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var next = enumerator.MoveNextAsync().AsTask();
            await PublishJetStreamAsync(fixture.Url, subjectB, """{"id":3}"""u8.ToArray(), cts.Token);

            Assert.True(await next);
            using var resumed = enumerator.Current;
            Assert.Equal("""{"id":3}""", Encoding.UTF8.GetString(resumed.Body));
        }
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
                .Subscribe(EmptySubscriptionFieldContext.Instance, [subject], cts.Token)
                .GetAsyncEnumerator(cts.Token);
            _ = enumerator.MoveNextAsync().AsTask();
            await Task.Delay(250, cts.Token);

            await broker.DisposeAsync();
        }

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .Subscribe(EmptySubscriptionFieldContext.Instance, [subject], cts.Token)
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
