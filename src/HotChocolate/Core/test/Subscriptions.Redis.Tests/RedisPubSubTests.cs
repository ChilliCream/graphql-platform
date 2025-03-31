using HotChocolate.Tests;
using Moq;
using Squadron;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Redis;

public class RedisPubSubTests : IClassFixture<RedisResource>
{
    private readonly RedisPubSub _redisPubSub;

    private readonly Mock<IConnectionMultiplexer> _connectionMock = new();
    private readonly Mock<ISubscriber> _subscriberMock = new();

    public RedisPubSubTests(RedisResource redisResource, ITestOutputHelper outputHelper)
    {
        var connectionMultiplexer = redisResource.GetConnection();

        _connectionMock
            .Setup(x => x.GetSubscriber(It.IsAny<object?>()))
            .Returns(_subscriberMock.Object);

        // Have to use the real implementation since ChannelMessageQueue is internal
        _subscriberMock
            .Setup(x => x.SubscribeAsync(It.IsAny<RedisChannel>(), CommandFlags.None))
            .Returns((RedisChannel channel, CommandFlags cf) => connectionMultiplexer.GetSubscriber().SubscribeAsync(channel, cf));

        _subscriberMock
            .Setup(x => x.UnsubscribeAsync(It.IsAny<RedisChannel>(), null, CommandFlags.None))
            .Returns((RedisChannel channel,  Action<RedisChannel,RedisValue>? handler, CommandFlags cf) => connectionMultiplexer.GetSubscriber().UnsubscribeAsync(channel, handler, cf));

        _redisPubSub = new RedisPubSub(
            _connectionMock.Object,
            new DefaultJsonMessageSerializer(),
            new SubscriptionOptions(),
            new SubscriptionTestDiagnostics(outputHelper));
    }

    [Fact]
    public async Task Unsubscribe_Should_UnsubscribeAsync()
    {
        var subscription = await _redisPubSub.SubscribeAsync<TestMessage>("test_topic");

        await subscription.DisposeAsync();

        _subscriberMock.Verify(
            x => x.UnsubscribeAsync(It.IsAny<RedisChannel>(), null, CommandFlags.None),
            Times.Once);
    }

    private record TestMessage();
}
