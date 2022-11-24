using System;
using System.Threading;
using System.Threading.Tasks;
using AlterNats;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using MessagePack;
using Squadron;

namespace HotChocolate.Subscriptions.Nats;

public class NatsIntegrationTests : IClassFixture<NatsResource>
{
    private const int _timeout = 5000;
    private readonly NatsResource _natsResource;

    public NatsIntegrationTests(NatsResource natsResource)
    {
        _natsResource = natsResource;
    }

    [Fact]
    public async Task Subscribe_Infer_Topic()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);

        IServiceProvider services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddLogging()
            .AddNatsSubscriptions(new SubscriptionOptions { TopicPrefix = "Subscribe_Infer_Topic" })
            .AddGraphQL()
            .AddSubscriptionType<Subscription>()
            .ModifyOptions(o => o.StrictValidation = false)
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
        Task.Factory.StartNew(
            async () =>
            {
                await Task.Delay(50, cts.Token);
                await sender.SendAsync("OnMessage", "bar", cts.Token);
                await sender.CompleteAsync("OnMessage");
            },
            TaskCreationOptions.LongRunning);

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
        using var cts = new CancellationTokenSource(_timeout);

        await using var services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddLogging()
            .AddNatsSubscriptions(new SubscriptionOptions { TopicPrefix = "Subscribe_Static_Topic" })
            .AddGraphQL()
            .AddSubscriptionType<Subscription2>()
            .ModifyOptions(o => o.StrictValidation = false)
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
        Task.Factory.StartNew(
            async () =>
            {
                await Task.Delay(50, cts.Token);
                await sender.SendAsync("OnMessage", new Foo { Bar = "Hello" }, cts.Token);
                await sender.CompleteAsync("OnMessage");
            },
            TaskCreationOptions.LongRunning);

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
        using var cts = new CancellationTokenSource(_timeout);

        await using var services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddLogging()
            .AddNatsSubscriptions(new SubscriptionOptions { TopicPrefix = "Subscribe_Topic_With_Arguments" })
            .AddGraphQL()
            .AddSubscriptionType<Subscription3>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();
        var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        var resultA = await executor.ExecuteAsync(
            "subscription { onMessage(arg:\"a\") }",
            cts.Token);

        var resultB = await executor.ExecuteAsync(
            "subscription { onMessage(arg:\"b\") }",
            cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStreamA = resultA.ExpectResponseStream();
        var resultsA = responseStreamA.ReadResultsAsync();

        await using var responseStreamB = resultB.ExpectResponseStream();
        var resultsB = responseStreamB.ReadResultsAsync();

        // assert
        Task.Factory.StartNew(
            async () =>
            {
                await Task.Delay(50, cts.Token);
                await sender.SendAsync("OnMessage_a", "abc", cts.Token);
                await sender.CompleteAsync("OnMessage_a");
            },
            TaskCreationOptions.LongRunning);

        var snapshot = new Snapshot();

        await foreach (var response in resultsA.WithCancellation(cts.Token))
        {
            snapshot.Add(response, name: "From Stream A");
        }

        Task.Factory.StartNew(
            async () =>
            {
                await Task.Delay(50, cts.Token);
                await sender.SendAsync("OnMessage_b", "def", cts.Token);
                await sender.CompleteAsync("OnMessage_b");
            },
            TaskCreationOptions.LongRunning);

        await foreach (var response in resultsB.WithCancellation(cts.Token))
        {
            snapshot.Add(response, name: "From Stream B");
        }

        snapshot.MatchInline(
            @"From Stream A
            ---------------
            {
              ""data"": {
                ""onMessage"": ""abc""
              }
            }
            ---------------

            From Stream B
            ---------------
            {
              ""data"": {
                ""onMessage"": ""def""
              }
            }
            ---------------
            ");
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

    public class FooType : ObjectType<Foo>
    {
    }

    public class Foo
    {
        public string? Bar { get; set; }
    }
}
