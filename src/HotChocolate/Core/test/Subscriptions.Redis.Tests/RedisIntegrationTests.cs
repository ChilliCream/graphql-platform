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
        using var cts = new CancellationTokenSource(_timeout);

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => _connection,
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
            cancellationToken: cts.Token)
            .ConfigureAwait(false);;

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);;

        // assert
        await sender.SendAsync("OnMessage", "bar", cts.Token).ConfigureAwait(false);;
        await sender.CompleteAsync("OnMessage").ConfigureAwait(false);;

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token).ConfigureAwait(false))
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
        using var cts = new CancellationTokenSource(_timeout);

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription2>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => _connection,
                options: new SubscriptionOptions { TopicPrefix = nameof(Subscribe_Static_Topic) })
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage { bar } }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);;

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);;

        // assert
        await sender.SendAsync("OnMessage", new Foo { Bar = "Hello" }, cts.Token)
            .ConfigureAwait(false);;
        await sender.CompleteAsync("OnMessage").ConfigureAwait(false);;

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token).ConfigureAwait(false))
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
        using var cts = new CancellationTokenSource(_timeout);

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription3>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => _connection,
                options: new SubscriptionOptions
                {
                    TopicPrefix = nameof(Subscribe_Topic_With_Arguments)
                })
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token).ConfigureAwait(false);
        await sender.CompleteAsync("OnMessage_a").ConfigureAwait(false);

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token).ConfigureAwait(false))
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
}
