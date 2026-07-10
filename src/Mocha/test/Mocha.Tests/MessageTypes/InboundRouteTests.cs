using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Tests for the condition that <see cref="InboundRoute.Initialize"/> derives for a route, covering
/// the default type condition, the non-null invariant for all routes, and the no-match reply route.
/// </summary>
public class InboundRouteTests
{
    [Fact]
    public void Initialize_Should_DeriveMessageTypeCondition_When_SubscribeRoute()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<RouteEventHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(RouteEventHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.IsType<MessageTypeCondition>(route.Condition);
    }

    [Fact]
    public void Initialize_Should_DeriveMessageTypeCondition_When_RequestRoute()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<RouteRequestHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(RouteRequestHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.IsType<MessageTypeCondition>(route.Condition);
    }

    [Fact]
    public void Initialize_Should_NeverLeaveConditionNull_When_RuntimeBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<RouteRequestHandler>());

        // assert
        Assert.All(runtime.Router.InboundRoutes, r => Assert.NotNull(r.Condition));
    }

    [Fact]
    public void Initialize_Should_DeriveNoMatchCondition_When_ReplyRouteHasNoMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<RouteRequestHandler>());

        // assert - the shared reply route carries no message type and never matches by the router
        var replyRoute = Assert.Single(runtime.Router.InboundRoutes, r => r.Kind == InboundRouteKind.Reply);
        Assert.Same(NoMatchCondition.Instance, replyRoute.Condition);
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

    public sealed class RouteEvent;

    public sealed class RouteRequest : IEventRequest<RouteResponse>;

    public sealed class RouteResponse;

    public sealed class RouteEventHandler : IEventHandler<RouteEvent>
    {
        public ValueTask HandleAsync(RouteEvent message, CancellationToken cancellationToken) => default;
    }

    public sealed class RouteRequestHandler : IEventRequestHandler<RouteRequest, RouteResponse>
    {
        public ValueTask<RouteResponse> HandleAsync(RouteRequest request, CancellationToken cancellationToken)
            => new(new RouteResponse());
    }
}
