using System.Collections.Immutable;
using System.Reflection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for the RoutingMiddleware which selects consumers for the current endpoint by evaluating
/// each route's condition against the received message.
/// </summary>
public class RoutingMiddlewareTests : ReceiveMiddlewareTestBase
{
    private static readonly ContextDataKey<string> s_correlationKey = new("test-correlation");

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
        // a type route does not match a message that never resolved a message type
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
    public async Task InvokeAsync_Should_NotAddConsumer_When_RouteHasNoMatchCondition()
    {
        // arrange - a no-match route (the RPC reply route) never selects its consumer
        var messageType = CreateTestMessageType();
        var consumer = new StubConsumer();
        var route = CreateInboundRoute(NoMatchCondition.Instance, consumer);

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

    [Fact]
    public async Task InvokeAsync_Should_SelectHeaderRoute_When_MessageTypeIsNullButHeaderMatches()
    {
        // arrange - a header based route selects on envelope metadata alone, even with no message type
        var consumer = new StubConsumer();
        var condition = new HeaderPresentCondition<string>(s_correlationKey);
        var route = CreateInboundRoute(condition, consumer);

        var router = new MockMessageRouter();
        router.SetRoutes([route]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = null };
        context.Headers.SetMessageKind(MessageKind.Reply);
        context.Headers.Set(s_correlationKey, "abc");
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Contains(consumer, feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_AddBothRoutes_When_TypeRouteAndHeaderRouteCoexist()
    {
        // arrange - a reply that matches both an ordinary type route and a header based reply route
        var messageType = CreateTestMessageType();
        var typeConsumer = new StubConsumer();
        var sagaConsumer = new StubConsumer();
        var typeRoute = CreateInboundRoute(messageType, typeConsumer);
        var sagaRoute = CreateInboundRoute(new HeaderPresentCondition<string>(s_correlationKey), sagaConsumer);

        var router = new MockMessageRouter();
        router.SetRoutes([typeRoute, sagaRoute]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = messageType };
        context.Headers.SetMessageKind(MessageKind.Reply);
        context.Headers.Set(s_correlationKey, "abc");
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Equal(2, feature.Consumers.Count);
        Assert.Contains(typeConsumer, feature.Consumers);
        Assert.Contains(sagaConsumer, feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_AddConsumerOnce_When_TwoRoutesShareConsumer()
    {
        // arrange - the same consumer is bound to two matching routes; the consumer set dedups it
        var messageType = CreateTestMessageType();
        var consumer = new StubConsumer();
        var route1 = CreateInboundRoute(messageType, consumer);
        var route2 = CreateInboundRoute(new HeaderPresentCondition<string>(s_correlationKey), consumer);

        var router = new MockMessageRouter();
        router.SetRoutes([route1, route2]);

        var middleware = new RoutingMiddleware(router);
        var context = new StubReceiveContext { MessageType = messageType };
        context.Headers.SetMessageKind(MessageKind.Reply);
        context.Headers.Set(s_correlationKey, "abc");
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Single(feature.Consumers);
        Assert.Contains(consumer, feature.Consumers);
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

    private static InboundRoute CreateInboundRoute(MessageType messageType, Consumer consumer)
        => CreateInboundRoute(new MessageTypeCondition(messageType), consumer, messageType);

    private static InboundRoute CreateInboundRoute(RouteCondition condition, Consumer consumer)
        => CreateInboundRoute(condition, consumer, messageType: null);

    private static InboundRoute CreateInboundRoute(
        RouteCondition condition,
        Consumer consumer,
        MessageType? messageType)
    {
        var route = new InboundRoute();
        SetPrivateProperty(route, nameof(InboundRoute.MessageType), messageType);
        SetPrivateProperty(route, nameof(InboundRoute.Consumer), consumer);
        SetPrivateProperty(route, nameof(InboundRoute.Condition), condition);
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
