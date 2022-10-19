using System;
using System.Threading;
using System.Threading.Tasks;
using AlterNats;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using MessagePack;
using Squadron;

namespace HotChocolate.Subscriptions.Nats;

public class NatsIntegrationTests
    : IClassFixture<NatsResource>
{
    private readonly NatsResource _natsResource;

    public NatsIntegrationTests(NatsResource natsResource)
    {
        _natsResource = natsResource;
    }

    [Fact]
    public async Task SubscribeAndCompleteSimple()
    {
        // arrange
        IServiceProvider services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddLogging()
            .AddNatsSubscriptions(prefix: "test")
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("foo")
                .Field("a")
                .Resolve("b"))
            .AddSubscriptionType<Subscription>()
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();
        var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync();

        var cts = new CancellationTokenSource(10000);

        // act
        var result = await executor.ExecuteAsync(
            "subscription { onMessage }",
            cts.Token);

        var stream = (IResponseStream)result;

        // assert
        await sender.SendAsync("OnMessage", "bar", cts.Token);
        await sender.CompleteAsync("OnMessage");

        await foreach (var response in stream.ReadResultsAsync()
            .WithCancellation(cts.Token))
        {
            Assert.Null(response.Errors);
            Assert.Equal("bar", response.Data!["onMessage"]);
        }

        await result.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAndCompleteWithComplexMessage()
    {
        // arrange
        var services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddLogging()
            .AddNatsSubscriptions(prefix: "test")
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("foo")
                .Field("a")
                .Resolve("b"))
            .AddSubscriptionType<Subscription2>()
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();
        var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync();

        var cts = new CancellationTokenSource(10000);

        // act
        var result = (IResponseStream)await executor.ExecuteAsync(
            "subscription { onMessage { bar } }",
            cts.Token);

        // assert
        await sender.SendAsync("OnMessage", new Foo { Bar = "Hello" }, cts.Token);
        await sender.CompleteAsync("OnMessage");

        await foreach (var response in result.ReadResultsAsync().WithCancellation(cts.Token))
        {
            Assert.Null(response.Errors);
            Assert.Contains<ObjectFieldResult>(((ObjectResult)response.Data!["onMessage"]),
                t => t.Name == "bar" && t.Value.ToString() == "Hello");
        }

        await result.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAndCompleteWithComplexTopic()
    {
        // arrange
        var services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddLogging()
            .AddNatsSubscriptions(prefix: "test")
            .AddGraphQL()
            .AddQueryType(d => d
                .Name("foobar")
                .Field("a")
                .Resolve("b"))
            .AddSubscriptionType<Subscription2Type>()
            .Services
            .BuildServiceProvider();

        var sender = services.GetRequiredService<ITopicEventSender>();
        var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync();

        var cts = new CancellationTokenSource(10000);

        // act
        var result = await executor.ExecuteAsync(
            @"subscription { onMessage2(arg1: { arg1: ""foo"", arg2: 42 }) { bar } }",
            cts.Token);

        // assert
        var topic = new Message2Input("foo", 42);
        await sender.SendAsync(topic, new Foo { Bar = "Hello" }, cts.Token);
        await sender.CompleteAsync(topic);

        await foreach (var response in ((IResponseStream) result).ReadResultsAsync().WithCancellation(cts.Token))
        {
            Assert.Null(response.Errors);
            Assert.Contains<ObjectFieldResult>(((ObjectResult)response.Data!["onMessage2"]),
                t => t.Name == "bar" && t.Value.ToString() == "Hello");
        }

        await result.DisposeAsync();
    }

    [Fact]
    public void SubscribeWithInvalidPrefixShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new ServiceCollection()
                .AddNats(poolSize: 1, options => options with
                {
                    Url = _natsResource.NatsConnectionString
                })
                .AddLogging()
                .AddNatsSubscriptions(prefix: "test.")
                .AddGraphQL()
                .AddQueryType(d => d
                    .Name("foo")
                    .Field("a")
                    .Resolve("b"))
                .AddSubscriptionType<Subscription>()
                .Services
                .BuildServiceProvider();
        });
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

        [Subscribe]
        public Foo OnMessage2([Topic]Message2Input arg1, [EventMessage] Foo message) => message;
    }

    public class Message2InputType : InputObjectType<Message2Input>
    {
    }

    public class FooType : ObjectType<Foo>
    {
    }

    [MessagePackObject]
    public record Message2Input([property:Key(0)] string Arg1, [property:Key(1)] int Arg2);

    public class Subscription2Type
        : ObjectType<Subscription2>
    {
        protected override void Configure(IObjectTypeDescriptor<Subscription2> descriptor)
        {
            descriptor
                .Field(t => t.OnMessage2(default, default))
                .SubscribeToTopic<Message2Input, Foo>("arg1")
                .Type<NonNullType<FooType>>()
                .Argument("arg1", a => a.Type<NonNullType<Message2InputType>>());
        }
    }
    public class Foo
    {
        public string? Bar { get; set; }
    }
}
