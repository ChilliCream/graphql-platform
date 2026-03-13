using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
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
    public async Task Subscribe_Union_Field_In_Payload()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var services = CreateServer(
            builder => builder
                .AddSubscriptionType<UnionPayloadSubscription>()
                .AddType<UnionTextMessage>()
                .AddType<UnionCodeMessage>()
                .ModifyOptions(o => o.StrictValidation = false));
        var sender = services.GetRequiredService<ITopicEventSender>();

        var result = await services.ExecuteRequestAsync(
            """
            subscription {
              onUnionPayload {
                message {
                  __typename
                  ... on UnionTextMessage {
                    text
                  }
                }
              }
            }
            """,
            cancellationToken: cts.Token);

        await using var responseStream = result.ExpectResponseStream();
        var responses = responseStream.ReadResultsAsync().ConfigureAwait(false);

        await sender.SendAsync(
            "OnUnionPayload",
            new UnionPayloadEnvelope
            {
                Message = new UnionTextMessage { Text = "from-redis" }
            },
            cts.Token);
        await sender.CompleteAsync("OnUnionPayload");

        var snapshot = new Snapshot();

        await foreach (var response in responses.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response);
        }

        snapshot.MatchInline(
            """
            {
              "data": {
                "onUnionPayload": {
                  "message": {
                    "__typename": "UnionTextMessage",
                    "text": "from-redis"
                  }
                }
              }
            }
            """);
    }

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

        var activeChannelsAfterUnsubscribe =
            WaitForChannelRemoval(cts, activeChannelsAfterSubscribe.Length, GetActiveChannelsAsync);
        Assert.True(activeChannelsAfterSubscribe.Length > activeChannelsAfterUnsubscribe);
    }

    public static int WaitForChannelRemoval(
        CancellationTokenSource cts,
        int currentlyActiveChannels,
        Func<Task<RedisResult[]>> getActiveChannelsAsync)
    {
        var activeChannelsAfterUnsubscribe = 0;
        var channelRemovedEvent = new ManualResetEventSlim(false);

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var activeChannels = await getActiveChannelsAsync();
                if (activeChannels.Length < currentlyActiveChannels)
                {
                    activeChannelsAfterUnsubscribe = activeChannels.Length;
                    channelRemovedEvent.Set();
                    break;
                }

                await Task.Delay(100, cts.Token);
            }
        }, cts.Token);

        channelRemovedEvent.Wait(cts.Token);
        return activeChannelsAfterUnsubscribe;
    }

    private async Task<RedisResult[]> GetActiveChannelsAsync()
    {
        return (RedisResult[])(await _redisResource.GetConnection().GetDatabase().ExecuteAsync("PUBSUB", "CHANNELS"))!;
    }

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
        => graphqlBuilder.AddRedisSubscriptions(_ => _redisResource.GetConnection());

    [UnionType]
    public interface IUnionMessage;

    public sealed class UnionTextMessage : IUnionMessage
    {
        public string Text { get; set; } = default!;
    }

    public sealed class UnionCodeMessage : IUnionMessage
    {
        public int Code { get; set; }
    }

    public sealed class UnionPayloadEnvelope
    {
        public IUnionMessage Message { get; set; } = default!;
    }

    public sealed class UnionPayloadSubscription
    {
        [Topic("OnUnionPayload")]
        [Subscribe]
        public UnionPayloadEnvelope OnUnionPayload([EventMessage] UnionPayloadEnvelope message)
            => message;
    }
}
