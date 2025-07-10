using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Redis;

public class RedisIntegrationTests : SubscriptionIntegrationTestBase, IClassFixture<RedisResource>
{
    private readonly RedisResource _redisResource;

    public RedisIntegrationTests(RedisResource redisResource, ITestOutputHelper output)
        : base(output)
    {
        _redisResource = redisResource;
    }

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
    public async Task Unsubscribe_Should_RemoveChannel()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var services = CreateServer<Subscription>();

        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);

        var activeChannelsAfterSubscribe = await GetActiveChannelsAsync();

        await result.DisposeAsync();

        var channelRemovedEvent = new ManualResetEventSlim(false);

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var activeChannels = await GetActiveChannelsAsync();
                if (activeChannels.Length < activeChannelsAfterSubscribe.Length)
                {
                    channelRemovedEvent.Set();
                    break;
                }

                await Task.Delay(100, cts.Token);
            }
        }, cts.Token);

        channelRemovedEvent.Wait(cts.Token);
    }

    private async Task<RedisResult[]> GetActiveChannelsAsync()
    {
        return (RedisResult[])(await _redisResource.GetConnection().GetDatabase().ExecuteAsync("PUBSUB", "CHANNELS"))!;
    }

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
        => graphqlBuilder.AddRedisSubscriptions(_ => _redisResource.GetConnection());
}
