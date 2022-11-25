using System;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis;

public class RedisIntegrationTests : IClassFixture<RedisResource>
{
    private const int _timeout = 5000;
    private readonly ConnectionMultiplexer _connection;

    public RedisIntegrationTests(RedisResource redisResource)
    {
        _connection = redisResource.GetConnection();
    }

    [Fact]
    public async Task Subscribe_Infer_Topic()
    {
        // arrange
        await using var redis = await CreateRedisResource();
        using var cts = new CancellationTokenSource(_timeout);
        var connection = redis.GetConnection();

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => connection,
                options: new SubscriptionOptions
                {
                    TopicPrefix = nameof(Subscribe_Infer_Topic)
                })
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync();

        // assert
        await sender.SendAsync("OnMessage", "bar", cts.Token);
        await sender.CompleteAsync("OnMessage");

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token))
        {
            snapshot.Add(response);
        }

        snapshot.MatchInline(
            @"{
              ""data"": {
                ""onMessage"": ""bar""
              }
            }");
    }

    [Fact]
    public async Task Subscribe_Static_Topic()
    {
        // arrange
        await using var redis = await CreateRedisResource();
        using var cts = new CancellationTokenSource(_timeout);
        var connection = redis.GetConnection();

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription2>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => connection,
                options: new SubscriptionOptions { TopicPrefix = nameof(Subscribe_Static_Topic) })
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage { bar } }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync();

        // assert
        await sender.SendAsync("OnMessage", new Foo { Bar = "Hello" }, cts.Token);
        await sender.CompleteAsync("OnMessage");

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token))
        {
            snapshot.Add(response);
        }

        snapshot.MatchInline(
            @"{
              ""data"": {
                ""onMessage"": {
                  ""bar"": ""Hello""
                }
              }
            }");
    }

    [Fact]
    public async Task Subscribe_Topic_With_Arguments()
    {
        // arrange
        await using var redis = await CreateRedisResource();
        using var cts = new CancellationTokenSource(_timeout);
        var connection = redis.GetConnection();

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription3>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => connection,
                options: new SubscriptionOptions
                {
                    TopicPrefix = nameof(Subscribe_Topic_With_Arguments)
                })
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();
        var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        var resultA = await executor.ExecuteAsync(
            "subscription { onMessage(arg:\"a\") }",
            cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStreamA = resultA.ExpectResponseStream();
        var resultsA = responseStreamA.ReadResultsAsync();

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token);
        await sender.CompleteAsync("OnMessage_a");

        var snapshot = new Snapshot();

        await foreach (var response in resultsA.WithCancellation(cts.Token))
        {
            snapshot.Add(response, name: "From Stream A");
        }

        snapshot.MatchInline(
            @"{
              ""data"": {
                ""onMessage"": ""abc""
              }
            }");
    }

    public class Subscription
    {
        [Subscribe]
        public string OnMessage([EventMessage] string message) => message;
    }

    public class Subscription2
    {
        [Topic("OnMessage")]
        [Subscribe]
        public Foo OnMessage([EventMessage] Foo message) => message;
    }

    public class Subscription3
    {
        [Topic("OnMessage_{arg}")]
        [Subscribe]
        public string OnMessage(string arg, [EventMessage] string message) => message;
    }

    public class FooType : ObjectType<Foo> { }

    public class Foo
    {
        public string? Bar { get; set; }
    }

    private async Task<RedisResource> CreateRedisResource()
    {
        var res = new RedisResource();
        await res.InitializeAsync();
        return res;
    }
}
