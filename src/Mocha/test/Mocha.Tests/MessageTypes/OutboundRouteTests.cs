using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Tests for the <see cref="OutboundRoute.HasExplicitDestination"/> flag, verifying that it is set
/// only when the destination is explicitly configured and not when backfilled from the endpoint
/// address.
/// </summary>
public class OutboundRouteTests
{
    [Fact]
    public void HasExplicitDestination_Should_BeTrue_When_DestinationConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.Host(h => h.ServiceName("orders"));
            b.AddMessage<TestEvent>(m => m.Publish(r => r.ToQueue("explicit-queue")));
        });

        // assert
        var route = runtime.Router.OutboundRoutes.Single(r => r.MessageType.RuntimeType == typeof(TestEvent));
        Assert.True(route.HasExplicitDestination);
        Assert.NotNull(route.Destination);
        Assert.Equal("queue:explicit-queue", route.Destination.ToString());
        Assert.Equal(
            MochaUrn.OutboundRoute("orders", "publish", route.MessageType.Identity, route.Endpoint.Name),
            route.Urn);
    }

    [Fact]
    public void HasExplicitDestination_Should_BeFalse_When_DestinationBackfilledFromEndpoint()
    {
        // arrange & act
        // Configure a message type with no explicit destination and ensure it gets connected to an endpoint
        // so the destination will be backfilled from the endpoint address
        var runtime = CreateRuntime(b => b.AddEventHandler<ImplicitDestinationEventHandler>());

        // assert
        // The inbound route for ImplicitDestinationEvent is created by the handler
        Assert.Single(
            runtime.Router.InboundRoutes,
            r => r.MessageType?.RuntimeType == typeof(ImplicitDestinationEvent));

        // The matching outbound route is created implicitly from the inbound route
        var outboundRoute = Assert.Single(
            runtime.Router.OutboundRoutes,
            r => r.MessageType.RuntimeType == typeof(ImplicitDestinationEvent));

        // This outbound route was not explicitly configured, so the flag should be false
        Assert.False(outboundRoute.HasExplicitDestination);
    }

    [Fact]
    public void Urn_Should_UseConnectedEndpoint_When_DestinationIsImplicit()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.Host(h => h.ServiceName("orders"));
            b.AddEventHandler<ImplicitDestinationEventHandler>();
        });

        // assert
        var route = Assert.Single(
            runtime.Router.OutboundRoutes,
            r => r.MessageType.RuntimeType == typeof(ImplicitDestinationEvent));

        Assert.True(route.IsCompleted);
        Assert.False(route.HasExplicitDestination);
        Assert.Equal(
            MochaUrn.OutboundRoute("orders", "publish", route.MessageType.Identity, route.Endpoint.Name),
            route.Urn);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    public sealed class TestEvent;

    public sealed class ImplicitDestinationEvent;

    public sealed class ImplicitDestinationEventHandler : IEventHandler<ImplicitDestinationEvent>
    {
        public ValueTask HandleAsync(ImplicitDestinationEvent message, CancellationToken cancellationToken) => default;
    }
}
