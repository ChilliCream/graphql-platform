using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions;

public abstract class SubscriptionIntegrationTestBase
{
    private static readonly int _timeout = Debugger.IsAttached ? 1000000 : 5000;
    private readonly ITestOutputHelper _output;

    protected SubscriptionIntegrationTestBase(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Fact]
    public virtual async Task Subscribe_Infer_Topic()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage", "bar", cts.Token);
        await sender.CompleteAsync("OnMessage");

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
    public virtual async Task Subscribe_Static_Topic()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription2>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage { bar } }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage", new Foo { Bar = "Hello", }, cts.Token);
        await sender.CompleteAsync("OnMessage");

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
    public virtual async Task Subscribe_Topic_With_Arguments()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription3>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token);
        await sender.CompleteAsync("OnMessage_a");

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

    [Fact]
    public virtual async Task Subscribe_Topic_With_Arguments_2_Subscriber()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription3>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result1 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token);

        var result2 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream1 = result1.ExpectResponseStream();
        var results1 = responseStream1.ReadResultsAsync().ConfigureAwait(false);

        await using var responseStream2 = result2.ExpectResponseStream();
        var results2 = responseStream2.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token);
        await sender.CompleteAsync("OnMessage_a");

        var snapshot = new Snapshot();

        await foreach (var response in results1.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response, name: "From Stream 1");
        }

        await foreach (var response in results2.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response, name: "From Stream 2");
        }

        snapshot.MatchInline(
            @"From Stream 1
            ---------------
            {
            ""data"": {
                ""onMessage"": ""abc""
            }
            }
            ---------------

            From Stream 2
            ---------------
            {
            ""data"": {
                ""onMessage"": ""abc""
            }
            }
            ---------------
            ");
    }

    [Fact]
    public virtual async Task Subscribe_Topic_With_Arguments_2_Topics()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription3>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result1 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token);

        var result2 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"b\") }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream1 = result1.ExpectResponseStream();
        var results1 = responseStream1.ReadResultsAsync().ConfigureAwait(false);

        await using var responseStream2 = result2.ExpectResponseStream();
        var results2 = responseStream2.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token);
        await sender.CompleteAsync("OnMessage_a");

        await sender.SendAsync("OnMessage_b", "def", cts.Token);
        await sender.CompleteAsync("OnMessage_b");

        var snapshot = new Snapshot();

        await foreach (var response in results1.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response, name: "From Stream 1");
        }

        await foreach (var response in results2.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response, name: "From Stream 2");
        }

        snapshot.MatchInline(
            @"From Stream 1
            ---------------
            {
            ""data"": {
                ""onMessage"": ""abc""
            }
            }
            ---------------

            From Stream 2
            ---------------
            {
            ""data"": {
                ""onMessage"": ""def""
            }
            }
            ---------------
            ");
    }

    [Fact]
    public virtual async Task Subscribe_Topic_With_2_Arguments()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription3>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage2(arg1: \"a\", arg2: \"b\") }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage2_a_b", "abc", cts.Token);
        await sender.CompleteAsync("OnMessage2_a_b");

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response, name: "From Stream A");
        }

        snapshot.MatchInline(
            @"{
              ""data"": {
                ""onMessage2"": ""abc""
              }
            }");
    }

    [Fact]
    public virtual async Task Subscribe_And_Complete_Topic()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription2>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage { bar } }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await Task.Delay(2000, cts.Token);
        await sender.CompleteAsync("OnMessage");

        await foreach (var unused in results.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            Assert.Fail("Should not have any messages.");
        }
    }

    [Fact]
    public virtual async Task Subscribe_And_Complete_Topic_With_ValueTypeMessage()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);
        await using var services = CreateServer<Subscription3>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage3 }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await Task.Delay(2000, cts.Token);
        await sender.CompleteAsync("OnMessage3");

        await foreach (var unused in results.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            Assert.Fail("Should not have any messages.");
        }
    }

    protected ServiceProvider CreateServer<TSubscriptionType>() where TSubscriptionType : class
        => CreateServer(builder =>
        {
            builder
                .AddSubscriptionType<TSubscriptionType>()
                .ModifyOptions(o => o.StrictValidation = false);
        });

    protected ServiceProvider CreateServer(Action<IRequestExecutorBuilder> configure)
    {
        var serviceCollection = new ServiceCollection();
        var graphqlBuilder = serviceCollection.AddGraphQL();

        graphqlBuilder.AddDiagnosticEventListener(_ => new SubscriptionTestDiagnostics(_output));

        configure(graphqlBuilder);
        ConfigurePubSub(graphqlBuilder);

        return serviceCollection.BuildServiceProvider();
    }

    protected abstract void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder);

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
        public string OnMessage(string arg, [EventMessage] string message)
            => message;

        [Topic("OnMessage2_{arg1}_{arg2}")]
        [Subscribe]
        public string OnMessage2(string arg1, string arg2, [EventMessage] string message)
            => message;

        [Topic("OnMessage3")]
        [Subscribe]
        public FooEnum OnMessage3([EventMessage] FooEnum message)
            => message;
    }

    public class Foo
    {
        public string? Bar { get; set; }
    }

    public enum FooEnum
    {
        Bar,
    }
}
