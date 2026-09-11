using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class ExplicitBindingTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task DiscoverTopology_Should_ClaimNothing_When_TheTransportBindsExplicitly()
    {
        // arrange
        // Explicit binding hands topology ownership to the caller, so the transport must not derive a
        // convention stream from the routes the way it does under implicit binding.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.BindExplicitly();

                // Declared, so the caller owns it. Under explicit binding it has to capture the fault
                // and skipped subjects too, since no convention stream is there to pick them up.
                nats.DeclareStream("OWNED")
                    .Subject("mocha.test-helpers.>")
                    .Subject("mocha.transport.nats.tests.owned-ep_error")
                    .Subject("mocha.transport.nats.tests.owned-ep_skipped");
                nats.Endpoint("owned-ep")
                    .Handler<OrderCreatedHandler>()
                    .Subject("mocha.test-helpers.order-created");
            })
            .BuildTestBusAsync();

        // assert
        var stream = Assert.Single(bus.Topology.Streams);

        Assert.Equal("OWNED", stream.Name);
        Assert.Equal(
            [
                "mocha.test-helpers.>",
                "mocha.transport.nats.tests.owned-ep_error",
                "mocha.transport.nats.tests.owned-ep_skipped"
            ],
            stream.Subjects);
    }

    [Fact]
    public async Task DiscoverTopology_Should_DeriveNoFilterSubjects_When_OneEndpointBindsExplicitly()
    {
        // arrange
        // The per-endpoint override governs what that endpoint binds, matching RabbitMQ, whose dispatch
        // side reads only the transport's mode. So the publish subject is still claimed here; what
        // changes is that the endpoint stops deriving a filter subject from its handler's route.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint("owned-ep")
                    .Handler<OrderCreatedHandler>()
                    .Subject("mocha.test-helpers.explicit-only")
                    .BindExplicitly();
            })
            .BuildTestBusAsync();

        // assert
        // Only what Subject() named. Under implicit binding the handler's own OrderCreated subject
        // would have been derived and added alongside it.
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == "owned-ep");

        Assert.Equal(["mocha.test-helpers.explicit-only"], consumer.FilterSubjects);
    }

    [Fact]
    public async Task DiscoverTopology_Should_DeriveFilterSubjects_When_TheEndpointBindsImplicitly()
    {
        // arrange
        // The counterpart to the test above, so the difference is attributable to the bind mode.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint("owned-ep")
                    .Handler<OrderCreatedHandler>()
                    .Subject("mocha.test-helpers.explicit-only");
            })
            .BuildTestBusAsync();

        // assert
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == "owned-ep");

        Assert.Equal(
            ["mocha.test-helpers.explicit-only", "mocha.test-helpers.order-created"],
            consumer.FilterSubjects.OrderBy(s => s, StringComparer.Ordinal));
    }

    [Fact]
    public async Task PublishAsync_Should_StillReachTheHandler_When_BindingExplicitlyOntoADeclaredStream()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.BindExplicitly();
                nats.DeclareStream("OWNED")
                    .Subject("mocha.test-helpers.>")
                    .Subject("mocha.transport.nats.tests.owned-ep_error")
                    .Subject("mocha.transport.nats.tests.owned-ep_skipped");
                nats.Endpoint("owned-ep")
                    .Handler<OrderCreatedHandler>()
                    .Subject("mocha.test-helpers.order-created");
            })
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Explicit binding onto a declared stream should still deliver.");
    }

    [Fact]
    public async Task Endpoint_Should_ConfigureItsConsumer_When_GivenAckSettings()
    {
        // arrange
        // These used to be reachable only through DeclareConsumer, which needs the derived durable name.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint("tuned-ep")
                    .Handler<OrderCreatedHandler>()
                    .AckWait(TimeSpan.FromSeconds(45))
                    .MaxAckPending(17)
                    .AckProgressEvery(TimeSpan.FromSeconds(15))
                    .DeliverFrom(ConsumerConfigDeliverPolicy.All);
            })
            .BuildTestBusAsync();

        // assert
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == "tuned-ep");

        Assert.Equal(17, consumer.MaxAckPending);
        Assert.Equal(TimeSpan.FromSeconds(15), consumer.AckProgressInterval);

        // AckWait and DeliverPolicy reach the server rather than the topology object, so read them back.
        var info = await fixture.JetStream.GetConsumerAsync(
            consumer.StreamName!,
            "tuned-ep",
            CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(45), info.Info.Config.AckWait);
        Assert.Equal(ConsumerConfigDeliverPolicy.All, info.Info.Config.DeliverPolicy);
    }

    [Fact]
    public async Task Consumer_Should_StartAtTheEnd_When_NoDeliveryPolicyIsDeclared()
    {
        // arrange
        // Streams are shared and retain independently of any consumer, so the default has to match a
        // newly bound RabbitMQ queue and skip what was published before the consumer existed.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        // assert
        var consumer = Assert.Single(bus.Topology.Consumers);

        var info = await fixture.JetStream.GetConsumerAsync(
            consumer.StreamName!,
            consumer.Name,
            CancellationToken.None);

        Assert.Equal(ConsumerConfigDeliverPolicy.New, info.Info.Config.DeliverPolicy);
    }
}
