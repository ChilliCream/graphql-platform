using System.Text;
using System.Threading.Channels;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions.AmazonSqs;

public sealed class AmazonSqsEventStreamBrokerTests(AmazonSqsFixture fixture)
    : IClassFixture<AmazonSqsFixture>
{
    [Fact]
    public async Task Subscribe_Should_DeliverPublishedEvent_When_SqsBrokerPublishes()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var topicArn = await fixture.CreateTopicAsync(topic, cts.Token);
        var queues = Channel.CreateUnbounded<string>();
        var services = CreateServices(
            queues,
            new Dictionary<string, string> { [topic] = topicArn },
            throwOnChannel: true);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("sqs");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var next = enumerator.MoveNextAsync().AsTask();
        await queues.Reader.ReadAsync(cts.Token);

        // act
        await fixture.PublishToTopicAsync(topicArn, """{"id":1}""", cts.Token);

        // assert
        Assert.True(await next);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
        Assert.Equal(0, message.Cursor.Length);
    }

    [Fact]
    public async Task Subscribe_Should_MergeAllQueuesIntoOneStream_When_MultipleTopicsSubscribed()
    {
        // arrange
        var topicA = CreateTopic();
        var topicB = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var topicArnA = await fixture.CreateTopicAsync(topicA, cts.Token);
        var topicArnB = await fixture.CreateTopicAsync(topicB, cts.Token);
        var queues = Channel.CreateUnbounded<string>();
        var services = CreateServices(
            queues,
            new Dictionary<string, string>
            {
                [topicA] = topicArnA,
                [topicB] = topicArnB
            });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("sqs");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topicA, topicB],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var events = ReadBodiesAsync(enumerator, count: 2, cts.Token);
        await ReadQueueUrlsAsync(queues.Reader, count: 2, cts.Token);

        // act
        await fixture.PublishToTopicAsync(topicArnA, """{"id":1}""", cts.Token);
        await fixture.PublishToTopicAsync(topicArnB, """{"id":2}""", cts.Token);

        // assert
        Assert.Equal(["""{"id":1}""", """{"id":2}"""], (await events).Order());
    }

    [Fact]
    public async Task Subscribe_Should_DeliverEveryEventToEachSubscriber_When_TwoConcurrentSubscribers()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var topicArn = await fixture.CreateTopicAsync(topic, cts.Token);
        var queues = Channel.CreateUnbounded<string>();
        var services = CreateServices(queues, new Dictionary<string, string> { [topic] = topicArn });
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var brokerA = factory.Create("sqs");
        await using var brokerB = factory.Create("sqs");

        await using var enumeratorA = brokerA
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        await using var enumeratorB = brokerB
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var nextA = enumeratorA.MoveNextAsync().AsTask();
        var nextB = enumeratorB.MoveNextAsync().AsTask();
        await ReadQueueUrlsAsync(queues.Reader, count: 2, cts.Token);

        // act
        await fixture.PublishToTopicAsync(topicArn, """{"id":1}""", cts.Token);

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
        var topic = CreateTopic();
        var services = CreateServices(Channel.CreateUnbounded<string>());
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("sqs");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // act
        var exception = Assert.Throws<InvalidEventMessageCursorException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: "anything",
                cts.Token));

        // assert
        Assert.Equal(InvalidEventMessageCursorException.DefaultMessage, exception.Message);
    }

    [Fact]
    public async Task Subscribe_Should_CloseConsumerCleanly_When_CancellationRequested()
    {
        // arrange
        var topic = CreateTopic();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var queues = Channel.CreateUnbounded<string>();
        var services = CreateServices(queues);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        await using var broker = factory.Create("sqs");

        await using var enumerator = broker
            .SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: null,
                cts.Token)
            .GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync().AsTask();
        var queueUrl = await queues.Reader.ReadAsync(cts.Token);

        // act
        await cts.CancelAsync();
        await broker.DisposeAsync();

        // assert
        Assert.False(await pending);
        Assert.Throws<ObjectDisposedException>(() =>
            broker.SubscribeAsync(
                EmptySubscriptionFieldContext.Instance,
                [topic],
                cursor: null,
                CancellationToken.None));

        using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await fixture.WaitForQueueDeletedAsync(queueUrl, cleanupCts.Token);
        Assert.False(await fixture.QueueExistsAsync(queueUrl, cleanupCts.Token));
    }

    private ServiceCollection CreateServices(
        Channel<string> queueUrls,
        IReadOnlyDictionary<string, string>? topicArns = null,
        bool throwOnChannel = false)
    {
        var services = new ServiceCollection();
        services.AddAmazonSqsEventStreamBroker(
            "sqs",
            o =>
            {
                o.ServiceUrl = fixture.ServiceUrl;
                o.Region = fixture.Region;
                o.Credentials = fixture.CreateCredentials();
                o.WaitTimeSeconds = 1;
                o.VisibilityTimeoutSeconds = 5;
                o.OnQueueReady = queueUrl => queueUrls.Writer.TryWrite(queueUrl);
                o.ResolveTopicArn = topicArns is null ? null : topic => topicArns[topic];

                if (throwOnChannel)
                {
                    o.CreateMessageChannel =
                        () => throw new InvalidOperationException(
                            "A channel should not be created for one topic.");
                }
            });
        return services;
    }

    private static async Task<string[]> ReadQueueUrlsAsync(
        ChannelReader<string> queueUrls,
        int count,
        CancellationToken cancellationToken)
    {
        var result = new string[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = await queueUrls.ReadAsync(cancellationToken);
        }

        return result;
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
