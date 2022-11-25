using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit.Abstractions;
using static System.Text.Json.JsonSerializer;

namespace HotChocolate.Subscriptions.Redis;

public class RedisIntegrationTests : IClassFixture<RedisResource>
{
    private static readonly int _timeout = Debugger.IsAttached ? 1000000 : 5000;
    private readonly RedisResource _redisResource;
    private readonly TestDiagnostics _testDiagnostics;

    public RedisIntegrationTests(RedisResource redisResource, ITestOutputHelper output)
    {
        _redisResource = redisResource;
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

        public override void Created(string topic)
            => _output.WriteLine($"Created: {topic}");

        public override void Connected(string topic)
            => _output.WriteLine($"Connected: {topic}");

        public override void Disconnected(string topic)
            => _output.WriteLine($"Disconnected: {topic}");

        public override void MessageProcessingError(string topic, Exception ex)
        {
            _output.WriteLine($"Error: {topic} {ex.Message} {ex.StackTrace} {ex.GetType().FullName}");
        }

        public override void Received(string topic, string message)
            => _output.WriteLine($"Received: {topic} {message}");

        public override void WaitForMessages(string topic)
            => _output.WriteLine($"WaitForMessages: {topic}");

        public override void Dispatched<T>(
            string topic,
            MessageEnvelope<T> message,
            int subscribers)
            => _output.WriteLine($"Dispatched: {topic} {Serialize(message)} {subscribers}");

        public override void Delayed<T>(
            string topic,
            MessageEnvelope<T> message,
            int subscribers)
            => _output.WriteLine($"Delayed: {topic} {Serialize(message)} {subscribers}");

        public override void TrySubscribe(string topic, int attempt)
            => _output.WriteLine($"TrySubscribe: {topic} {attempt}");

        public override void SubscribeSuccess(string topic)
            => _output.WriteLine($"Subscribe Successful: {topic}");

        public override void SubscribeFailed(string topic)
            => _output.WriteLine($"Subscribe Failed: {topic}");

        public override void Unsubscribe(string topic, int subscribers)
            => _output.WriteLine($"Unsubscribe: {topic} {subscribers}");

        public override void Close(string topic)
            => _output.WriteLine($"Close: {topic}");

        public override void Send<T>(string topic, MessageEnvelope<T> message)
            => _output.WriteLine($"Send: {topic} {Serialize(message)}");

        public override void ProviderInfo(string infoText)
            => _output.WriteLine($"Info: {infoText}");

        public override void ProviderTopicInfo(string topic, string infoText)
            => _output.WriteLine($"Info: {infoText}");
    }
}
