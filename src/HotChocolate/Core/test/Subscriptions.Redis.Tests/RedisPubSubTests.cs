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
        await using var connectionMultiplexer = _redisResource.GetConnection();

        _subscriberMock
            .Setup(x => x.SubscribeAsync(It.IsAny<RedisChannel>(), CommandFlags.None))
            .Returns((RedisChannel channel, CommandFlags flags) => connectionMultiplexer.GetSubscriber().SubscribeAsync(channel, flags));

        var subscription = await _redisPubSub.SubscribeAsync<TestMessage>("test_topic");

        await subscription.DisposeAsync();

        _subscriberMock.Verify(
            x => x.UnsubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel,RedisValue>?>(), CommandFlags.None),
            Times.Once);

        await connectionMultiplexer.GetSubscriber().UnsubscribeAllAsync();
    }

    private record TestMessage();
}
