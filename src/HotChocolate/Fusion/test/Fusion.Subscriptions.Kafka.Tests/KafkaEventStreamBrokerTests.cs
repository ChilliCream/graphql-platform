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
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

        // act
        await ProduceAsync(topic, """{"id":1}"""u8.ToArray(), cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.Equal($"{topic}:0:0", Encoding.UTF8.GetString(message.Cursor));
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
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topicA, topicB], cts.Token)
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
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cts.Token)
            .GetAsyncEnumerator(cts.Token);
        await using var enumeratorB = brokerB
            .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cts.Token)
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
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cts.Token)
                .GetAsyncEnumerator(cts.Token);
            var pending = enumerator.MoveNextAsync().AsTask();
            await WaitForAssignmentsAsync(assignments.Reader, count: 1, cts.Token);

            await broker.DisposeAsync();
            Assert.False(await pending);
        }

        await using (var broker = factory.Create(null))
        {
            await using var enumerator = broker
                .SubscribeAsync(EmptySubscriptionFieldContext.Instance, [topic], cts.Token)
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

    private ServiceCollection CreateServices(
        Channel<IReadOnlyList<TopicPartition>> assignments)
    {
        var services = new ServiceCollection();
        services.AddKafkaEventStreamBroker(
            configure: o =>
            {
                o.BootstrapServers = _fixture.BootstrapServers;
                o.AutoOffsetReset = AutoOffsetReset.Earliest;
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
        public static readonly EmptySubscriptionFieldContext Instance = new();

        private EmptySubscriptionFieldContext()
        {
        }

        public IReadOnlyDictionary<string, IValueNode> Arguments { get; } =
            new Dictionary<string, IValueNode>();
    }
}
