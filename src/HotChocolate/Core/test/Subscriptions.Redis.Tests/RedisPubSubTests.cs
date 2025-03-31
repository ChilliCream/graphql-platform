using HotChocolate.Tests;
using Moq;
using Squadron;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Redis;

public class RedisPubSubTests : IClassFixture<RedisResource>
{
    private readonly RedisResource _redisResource;
    private readonly RedisPubSub _redisPubSub;

    private readonly Mock<ISubscriber> _subscriberMock = new();

    public RedisPubSubTests(RedisResource redisResource, ITestOutputHelper outputHelper)
    {
        _redisResource = redisResource;

        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock
            .Setup(x => x.GetSubscriber(It.IsAny<object?>()))
            .Returns(_subscriberMock.Object);

        _redisPubSub = new RedisPubSub(
            connectionMock.Object,
            new DefaultJsonMessageSerializer(),
            new SubscriptionOptions(),
            new SubscriptionTestDiagnostics(outputHelper));
    }

    [Fact]
    public async Task Unsubscribe_Should_UnsubscribeAsync()
    {
        // Have to use the real implementation since ChannelMessageQueue is internal
        var connectionMultiplexer = _redisResource.GetConnection();

        RedisChannel subscribedChannel = "test_topic";
        _subscriberMock
            .Setup(x => x.SubscribeAsync(It.IsAny<RedisChannel>(), CommandFlags.None))
            .Returns((RedisChannel channel, CommandFlags flags) => connectionMultiplexer.GetSubscriber().SubscribeAsync(channel, flags))
            .Callback<RedisChannel, CommandFlags>((channel, _) => subscribedChannel = channel);

        _subscriberMock
            .Setup(x => x.UnsubscribeAsync(It.IsAny<RedisChannel>(), null, CommandFlags.None))
            .Returns((RedisChannel channel,  Action<RedisChannel,RedisValue>? handler, CommandFlags cf) => connectionMultiplexer.GetSubscriber().UnsubscribeAsync(channel, handler, cf));

        var subscription = await _redisPubSub.SubscribeAsync<TestMessage>("test_topic");

        await subscription.DisposeAsync();

        _subscriberMock.Verify(
            x => x.UnsubscribeAsync(subscribedChannel, null, CommandFlags.None),
            Times.Once);
    }

    private record TestMessage();
}
