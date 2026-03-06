using System.Collections.Immutable;
using System.Reflection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for the RoutingMiddleware which routes incoming messages to the appropriate consumers
/// based on message type matching.
/// </summary>
public class RoutingMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_AddConsumer_When_MessageTypeMatchesRoute()
    {
        // arrange
        var messageType = CreateTestMessageType();
        var consumer = new StubConsumer();
        var route = CreateInboundRoute(messageType, consumer);

        var router = new MockMessageRouter();
        router.SetRoutes([route]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = messageType };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Contains(consumer, feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotAddConsumer_When_NoRoutes()
    {
        // arrange
        var messageType = CreateTestMessageType();
        var router = new MockMessageRouter();
        router.SetRoutes([]); // empty routes

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = messageType };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Empty(feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_AddMultipleConsumers_When_MultipleRoutesMatch()
    {
        // arrange
        var messageType = CreateTestMessageType();
        var consumer1 = new StubConsumer();
        var consumer2 = new StubConsumer();
        var route1 = CreateInboundRoute(messageType, consumer1);
        var route2 = CreateInboundRoute(messageType, consumer2);

        var router = new MockMessageRouter();
        router.SetRoutes([route1, route2]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = messageType };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Equal(2, feature.Consumers.Count);
        Assert.Contains(consumer1, feature.Consumers);
        Assert.Contains(consumer2, feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_AlwaysCallNext()
    {
        // arrange
        var router = new MockMessageRouter();
        router.SetRoutes([]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = CreateTestMessageType() };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(tracker.WasInvoked, "Next should always be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_NotAddConsumers_When_MessageTypeIsNull()
    {
        // arrange
        var router = new MockMessageRouter();
        // Even if routes exist, null MessageType means no routing
        var consumer = new StubConsumer();
        router.SetRoutes([CreateInboundRoute(CreateTestMessageType(), consumer)]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = null };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Empty(feature.Consumers);
        Assert.True(tracker.WasInvoked, "Next should still be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_MatchOnEnclosedMessageTypes()
    {
        // arrange - route targets a base/enclosed type, context has a derived type
        var enclosedType = CreateTestMessageType();
        var derivedType = CreateTestMessageType(enclosedTypes: [enclosedType]);
        var consumer = new StubConsumer();
        var route = CreateInboundRoute(enclosedType, consumer);

        var router = new MockMessageRouter();
        router.SetRoutes([route]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = derivedType };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert - consumer should be added via enclosed type matching
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Contains(consumer, feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotAddConsumer_When_RouteMessageTypeIsNull()
    {
        // arrange - route without a message type should not match
        var messageType = CreateTestMessageType();
        var consumer = new StubConsumer();
        var route = CreateInboundRoute(null, consumer);

        var router = new MockMessageRouter();
        router.SetRoutes([route]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = messageType };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Empty(feature.Consumers);
    }

    private static MessageType CreateTestMessageType(ImmutableArray<MessageType>? enclosedTypes = null)
    {
        var mt = new MessageType();
        if (enclosedTypes.HasValue)
        {
            SetPrivateProperty(mt, nameof(MessageType.EnclosedMessageTypes), enclosedTypes.Value);
        }
        return mt;
    }

    private static InboundRoute CreateInboundRoute(MessageType? messageType, Consumer? consumer)
    {
        var route = new InboundRoute();
        SetPrivateProperty(route, nameof(InboundRoute.MessageType), messageType);
        SetPrivateProperty(route, nameof(InboundRoute.Consumer), consumer);
        return route;
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property!.SetValue(target, value);
    }

    private sealed class MockMessageRouter : IMessageRouter
    {
        private ImmutableHashSet<InboundRoute> _routes = [];

        public IReadOnlyList<InboundRoute> InboundRoutes => [.. _routes];
        public IReadOnlyList<OutboundRoute> OutboundRoutes => [];

        public void SetRoutes(IEnumerable<InboundRoute> routes) => _routes = [.. routes];

        public ImmutableHashSet<InboundRoute> GetInboundByEndpoint(ReceiveEndpoint endpoint) => _routes;

        public ImmutableHashSet<InboundRoute> GetInboundByMessageType(MessageType messageType) => _routes;

        public ImmutableHashSet<InboundRoute> GetInboundByConsumer(Consumer consumer) => _routes;

        public ImmutableHashSet<OutboundRoute> GetOutboundByMessageType(MessageType messageType) => [];

        public DispatchEndpoint GetEndpoint(
            IMessagingConfigurationContext context,
            MessageType messageType,
            OutboundRouteKind kind)
            => null!;

        public void AddOrUpdate(InboundRoute route) { }

        public void AddOrUpdate(OutboundRoute route) { }
    }

    private sealed class StubConsumer : Consumer
    {
        protected override ValueTask ConsumeAsync(IConsumeContext context) => ValueTask.CompletedTask;
    }
}
