using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Tests for the <see cref="OutboundRoute.HasExplicitDestination"/> flag, verifying that it is set only when the destination is explicitly configured and not when backfilled from the endpoint address.
/// </summary>
public class OutboundRouteTests
{
    [Fact]
    public void HasExplicitDestination_Should_BeTrue_When_DestinationConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
            b.AddMessage<TestEvent>(m => m.Publish(r => r.ToQueue("explicit-queue"))));

        // assert
        var route = runtime.Router.OutboundRoutes.Single(r => r.MessageType.RuntimeType == typeof(TestEvent));
        Assert.True(route.HasExplicitDestination);
        Assert.NotNull(route.Destination);
        Assert.Equal("queue:explicit-queue", route.Destination.ToString());
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
        var inboundRoute = runtime.Router.InboundRoutes.Single(r => r.MessageType?.RuntimeType == typeof(ImplicitDestinationEvent));

        // The matching outbound route is created implicitly from the inbound route
        var outboundRoute = runtime.Router.OutboundRoutes.Single(r => r.MessageType.RuntimeType == typeof(ImplicitDestinationEvent));

        // This outbound route was not explicitly configured, so the flag should be false
        Assert.False(outboundRoute.HasExplicitDestination);
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
