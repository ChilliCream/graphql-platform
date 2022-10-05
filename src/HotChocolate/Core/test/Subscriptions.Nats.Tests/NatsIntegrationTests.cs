using System;
using System.Threading;
using System.Threading.Tasks;
using AlterNats;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;
using Squadron;
using Xunit;

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
    public async Task SubscribeAndComplete()
    {
        // arrange
        IServiceProvider services = new ServiceCollection()
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString
            })
            .AddNatsSubscriptions()
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
        var result = (IResponseStream)await executor.ExecuteAsync(
            "subscription { onMessage }",
            cts.Token);

        // assert
        await sender.SendAsync("OnMessage", "bar", cts.Token);
        await sender.CompleteAsync("OnMessage");

        await foreach (var response in result.ReadResultsAsync()
            .WithCancellation(cts.Token))
        {
            Assert.Null(response.Errors);
            Assert.Equal("bar", response.Data!["onMessage"]);
        }

        await result.DisposeAsync();
    }

    //[Fact]
    // public async Task SubscribeAndComplete_GetMultiPlexerFromId()
    // {
    //     // arrange
    //     IServiceProvider services = new ServiceCollection()
    //         .AddSingleton<IConnectionMultiplexer>(_connection)
    //         .AddGraphQL()
    //         .AddRedisSubscriptions()
    //         .AddQueryType(d => d
    //             .Name("foo")
    //             .Field("a")
    //             .Resolve("b"))
    //         .AddSubscriptionType<Subscription>()
    //         .Services
    //         .BuildServiceProvider();
    //
    //     var sender = services.GetRequiredService<ITopicEventSender>();
    //     var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
    //     var executor = await executorResolver.GetRequestExecutorAsync();
    //
    //     var cts = new CancellationTokenSource(10000);
    //
    //     // act
    //     var result = (IResponseStream)await executor.ExecuteAsync(
    //         "subscription { onMessage }",
    //         cts.Token);
    //
    //     // assert
    //     await sender.SendAsync("OnMessage", "bar", cts.Token);
    //     await sender.CompleteAsync("OnMessage");
    //
    //     await foreach (var response in result.ReadResultsAsync()
    //         .WithCancellation(cts.Token))
    //     {
    //         Assert.Null(response.Errors);
    //         Assert.Equal("bar", response.Data!["onMessage"]);
    //     }
    //
    //     await result.DisposeAsync();
    // }

    public class FooType : InputObjectType
    {
        protected override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Abc");
            descriptor.Field("def").Type<StringType>();
        }
    }

    public class Subscription
    {
        [Subscribe]
        public string OnMessage([EventMessage] string message) => message;
    }
}
