using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit.Abstractions;
using static System.Text.Json.JsonSerializer;

namespace HotChocolate.Subscriptions.Redis;

public class IntegrationTestBase
{
    private static readonly int _timeout = Debugger.IsAttached ? 1000000 : 5000;
    private readonly TestDiagnostics _testDiagnostics;

    public IntegrationTestBase(ITestOutputHelper output)
    {
        _testDiagnostics = new TestDiagnostics(output);
    }

    [Fact]
    public async Task Subscribe_Infer_Topic()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);

        var connection = _redisResource.GetConnection();
        Assert.True(connection.IsConnected, "connection.IsConnected");

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
            .AddSingleton<ISubscriptionDiagnosticEvents>(_testDiagnostics)
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);
        ;

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);
        ;

        // assert
        await sender.SendAsync("OnMessage", "bar", cts.Token).ConfigureAwait(false);
        ;
        await sender.CompleteAsync("OnMessage").ConfigureAwait(false);
        ;

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

        var connection = _redisResource.GetConnection();
        Assert.True(connection.IsConnected, "connection.IsConnected");

        await using var services = new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType<Subscription2>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddRedisSubscriptions(
                _ => connection,
                options: new SubscriptionOptions { TopicPrefix = nameof(Subscribe_Static_Topic) })
            .Services
            .AddSingleton<ISubscriptionDiagnosticEvents>(_testDiagnostics)
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage { bar } }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);
        ;

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);
        ;

        // assert
        await sender.SendAsync("OnMessage", new Foo { Bar = "Hello" }, cts.Token)
            .ConfigureAwait(false);
        ;
        await sender.CompleteAsync("OnMessage").ConfigureAwait(false);
        ;

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

        var connection = _redisResource.GetConnection();
        Assert.True(connection.IsConnected, "connection.IsConnected");

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
            .AddSingleton<ISubscriptionDiagnosticEvents>(_testDiagnostics)
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

    [Fact]
    public async Task Subscribe_Topic_With_Arguments_2_Subscriber()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);

        var connection = _redisResource.GetConnection();
        Assert.True(connection.IsConnected, "connection.IsConnected");

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
            .AddSingleton<ISubscriptionDiagnosticEvents>(_testDiagnostics)
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result1 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);

        var result2 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream1 = result1.ExpectResponseStream();
        var results1 = responseStream1.ReadResultsAsync().ConfigureAwait(false);

        await using var responseStream2 = result2.ExpectResponseStream();
        var results2 = responseStream2.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token).ConfigureAwait(false);
        await sender.CompleteAsync("OnMessage_a").ConfigureAwait(false);

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
    public async Task Subscribe_Topic_With_Arguments_2_Topics()
    {
        // arrange
        using var cts = new CancellationTokenSource(_timeout);

        var connection = _redisResource.GetConnection();
        Assert.True(connection.IsConnected, "connection.IsConnected");

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
            .AddSingleton<ISubscriptionDiagnosticEvents>(_testDiagnostics)
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();

        // act
        var result1 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"a\") }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);

        var result2 = await services.ExecuteRequestAsync(
            "subscription { onMessage(arg: \"b\") }",
            cancellationToken: cts.Token)
            .ConfigureAwait(false);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream1 = result1.ExpectResponseStream();
        var results1 = responseStream1.ReadResultsAsync().ConfigureAwait(false);

        await using var responseStream2 = result2.ExpectResponseStream();
        var results2 = responseStream2.ReadResultsAsync().ConfigureAwait(false);

        // assert
        await sender.SendAsync("OnMessage_a", "abc", cts.Token).ConfigureAwait(false);
        await sender.CompleteAsync("OnMessage_a").ConfigureAwait(false);

        await sender.SendAsync("OnMessage_b", "def", cts.Token).ConfigureAwait(false);
        await sender.CompleteAsync("OnMessage_b").ConfigureAwait(false);

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

    protected ServiceProvider CreateServer<TSubscriptionType>()
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

        configure(graphqlBuilder);
        ConfigurePubSub(graphqlBuilder);


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
        public string OnMessage(string arg, [EventMessage] string message) => message;
    }

    public class FooType : ObjectType<Foo> { }

    public class Foo
    {
        public string? Bar { get; set; }
    }

    private sealed class TestDiagnostics : SubscriptionDiagnosticEventsListener
    {
        private readonly ITestOutputHelper _output;

        public TestDiagnostics(ITestOutputHelper output)
            => _output = output;

        public override void Created(string topicName)
            => _output.WriteLine($"Created: {topicName}");

        public override void Connected(string topicName)
            => _output.WriteLine($"Connected: {topicName}");

        public override void Disconnected(string topicName)
            => _output.WriteLine($"Disconnected: {topicName}");

        public override void MessageProcessingError(string topicName, Exception error)
        {
            _output.WriteLine($"Error: {topicName} {error.Message} {error.StackTrace} {error.GetType().FullName}");
        }

        public override void Received(string topicName, string serializedMessage)
            => _output.WriteLine($"Received: {topicName} {serializedMessage}");

        public override void WaitForMessages(string topicName)
            => _output.WriteLine($"WaitForMessages: {topicName}");

        public override void Dispatch<T>(
            string topicName,
            MessageEnvelope<T> message,
            int subscribers)
            => _output.WriteLine($"Dispatched: {topicName} {Serialize(message)} {subscribers}");

        public override void DelayedDispatch<T>(
            string topicName,
            MessageEnvelope<T> message,
            int subscribers)
            => _output.WriteLine($"Delayed: {topicName} {Serialize(message)} {subscribers}");

        public override void TrySubscribe(string topicName, int attempt)
            => _output.WriteLine($"TrySubscribe: {topicName} {attempt}");

        public override void SubscribeSuccess(string topicName)
            => _output.WriteLine($"Subscribe Successful: {topicName}");

        public override void SubscribeFailed(string topicName)
            => _output.WriteLine($"Subscribe Failed: {topicName}");

        public override void Unsubscribe(string topicName, int subscribers)
            => _output.WriteLine($"Unsubscribe: {topicName} {subscribers}");

        public override void Close(string topicName)
            => _output.WriteLine($"Close: {topicName}");

        public override void Send<T>(string topicName, MessageEnvelope<T> message)
            => _output.WriteLine($"Send: {topicName} {Serialize(message)}");

        public override void ProviderInfo(string infoText)
            => _output.WriteLine($"Info: {infoText}");

        public override void ProviderTopicInfo(string topicName, string infoText)
            => _output.WriteLine($"Info: {infoText}");
    }
}
