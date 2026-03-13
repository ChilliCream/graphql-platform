using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Redis;

public class RedisTopicPrefixIntegrationTests(RedisResource redisResource, ITestOutputHelper output)
    : SubscriptionIntegrationTestBase(output), IClassFixture<RedisResource>
{
    private const string TopicPrefix = "prefix:";

    [Fact]
    public override Task Subscribe_Infer_Topic()
        => base.Subscribe_Infer_Topic();

    [Fact]
    public override Task Subscribe_Static_Topic()
        => base.Subscribe_Static_Topic();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments()
        => base.Subscribe_Topic_With_Arguments();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments_2_Subscriber()
        => base.Subscribe_Topic_With_Arguments_2_Subscriber();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments_2_Topics()
        => base.Subscribe_Topic_With_Arguments_2_Topics();

    [Fact]
    public override Task Subscribe_Topic_With_2_Arguments()
        => base.Subscribe_Topic_With_2_Arguments();

    [Fact]
    public override Task Subscribe_And_Complete_Topic()
        => base.Subscribe_And_Complete_Topic();

    [Fact]
    public override Task Subscribe_And_Complete_Topic_With_ValueTypeMessage()
        => base.Subscribe_And_Complete_Topic_With_ValueTypeMessage();

    [Fact]
    public async Task Subscribe_Should_Create_Channel_With_Prefix()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var services = CreateServer<Subscription>();

        await using var result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);

        var activeChannels = await GetActiveChannelsAsync();
        Assert.NotEmpty(activeChannels);
    }

    [Fact]
    public async Task Unsubscribe_And_Reconnect_Topic()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var services = CreateServer<Subscription>();

        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);

        var activeChannelsAfterSubscribe = await GetActiveChannelsAsync();
        Assert.NotEmpty(activeChannelsAfterSubscribe);

        await result.DisposeAsync();

        var activeChannelsAfterUnsubscribe = RedisIntegrationTests.WaitForChannelRemoval(
            cts,
            activeChannelsAfterSubscribe.Length,
            GetActiveChannelsAsync);
        Assert.True(activeChannelsAfterSubscribe.Length > activeChannelsAfterUnsubscribe);

        // reconnect
        result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);
        activeChannelsAfterSubscribe = await GetActiveChannelsAsync();
        Assert.True(activeChannelsAfterSubscribe.Length > activeChannelsAfterUnsubscribe);

        await result.DisposeAsync();
    }

    private async Task<RedisResult[]> GetActiveChannelsAsync()
    {
        var channels = (RedisResult[])(await redisResource.GetConnection().GetDatabase().ExecuteAsync("PUBSUB", "CHANNELS"))!;
        return channels.Where(x => x.ToString()!.StartsWith(TopicPrefix)).ToArray();
    }

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
        => graphqlBuilder.AddRedisSubscriptions(_ => redisResource.GetConnection(), new SubscriptionOptions { TopicPrefix = TopicPrefix });
}
