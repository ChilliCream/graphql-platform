using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.TestHelpers;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class ReceivesTests
{
    [Fact]
    public void Receives_Should_BindHandlerToEndpoint_When_MessageTypeDeclared()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").Receives<OrderCreated>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "orders");

        Assert.NotNull(endpoint);
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        var route = Assert.Single(routes);
        Assert.Equal(endpoint, route.Endpoint);
    }

    [Fact]
    public void Receives_Should_BindAllHandlers_When_MultipleHandlersForSameType()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHandler<OrderCreatedHandler2>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").Receives<OrderCreated>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "orders");

        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.All(routes, r => Assert.Equal(endpoint, r.Endpoint));
    }

    [Fact]
    public void Receives_Should_FanOutToBothQueues_When_SameTypeDeclaredOnTwoEndpoints()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders-primary").Receives<OrderCreated>();
                t.Queue("orders-backup").Receives<OrderCreated>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var primaryEndpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "orders-primary");
        var backupEndpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "orders-backup");

        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, r => r.Endpoint == primaryEndpoint);
        Assert.Contains(routes, r => r.Endpoint == backupEndpoint);
    }

    [Fact]
    public void Receives_Should_Throw_When_NoHandlerRegistered()
    {
        // arrange & act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection()
                .AddMessageBus()
                .AddInMemory(t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("orders").Receives<OrderCreated>();
                })
                .BuildRuntime());

        Assert.Contains("No handler or consumer handles message type", exception.Message);
        Assert.Contains(typeof(OrderCreated).FullName!, exception.Message);
        Assert.Contains("orders", exception.Message);
    }

    [Fact]
    public void Receives_Should_SuppressNoRouteThrow_When_BindFromDeclared()
    {
        // arrange
        // No handler is registered, so no inbound route exists for OrderCreated.
        // The per-type BindFrom declares an explicit source topic that routes messages into the queue,
        // so the missing inbound route must not raise NoHandlerForMessageType.

        // act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders")
                    .Receives<OrderCreated>(r => r.BindFrom(new Uri("topic:orders")));
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "orders");

        Assert.NotNull(endpoint);
    }

    [Fact]
    public void Receives_Should_StillThrow_When_NoRouteAndNoBindFrom()
    {
        // arrange
        // No handler is registered and no per-type BindFrom is declared.
        // The missing inbound route must still raise NoHandlerForMessageType.

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection()
                .AddMessageBus()
                .AddInMemory(t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("orders").Receives<OrderCreated>();
                })
                .BuildRuntime());

        Assert.Contains("No handler or consumer handles message type", exception.Message);
        Assert.Contains(typeof(OrderCreated).FullName!, exception.Message);
    }

    [Fact]
    public void Consumer_Should_BindToBothEndpoints_When_MappedToTwoEndpoints()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<TestOrderConsumer>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders-a").Consumer<TestOrderConsumer>();
                t.Queue("orders-b").Consumer<TestOrderConsumer>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpointA = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "orders-a");
        var endpointB = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "orders-b");

        var routes = runtime.Router.InboundRoutes
            .Where(r => r.Consumer?.Identity == typeof(TestOrderConsumer))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, r => r.Endpoint == endpointA);
        Assert.Contains(routes, r => r.Endpoint == endpointB);
    }

    [Fact]
    public async Task Receives_Should_DeliverMessages_When_Published()
    {
        // arrange
        var recorder = new MessageRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        var provider = await services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").Receives<OrderCreated>();
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "123" }, CancellationToken.None);
        var received = await recorder.WaitAsync(TimeSpan.FromSeconds(5));

        // assert
        Assert.True(received);
        Assert.Single(recorder.Messages);
        var message = Assert.IsType<OrderCreated>(recorder.Messages.First());
        Assert.Equal("123", message.OrderId);
    }

    [Fact]
    public void Receives_Should_NotCreateDuplicateRoutes_When_SameTypeDeclaredOnThreeEndpoints()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders-1").Receives<OrderCreated>();
                t.Queue("orders-2").Receives<OrderCreated>();
                t.Queue("orders-3").Receives<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(3, routes.Count);
        Assert.Equal(3, routes.Select(r => r.Endpoint).Distinct().Count());
    }

    [Fact]
    public void Receives_Should_PreserveCondition_When_FannedOut()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders-primary").Receives<OrderCreated>();
                t.Queue("orders-backup").Receives<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.Same(routes[0].Condition, routes[1].Condition);
    }

    [Fact]
    public async Task Receives_Should_DeliverToBothQueues_When_FannedOut()
    {
        // arrange
        var recorder = new MessageRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        var provider = await services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders-primary").Receives<OrderCreated>();
                t.Queue("orders-backup").Receives<OrderCreated>();
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "123" }, CancellationToken.None);
        var received = await recorder.WaitAsync(TimeSpan.FromSeconds(5), expectedCount: 2);

        // assert
        Assert.True(received);
        Assert.Equal(2, recorder.Messages.Count);
    }

    [Fact]
    public void Receives_Should_Throw_When_TypeIsReplyType()
    {
        // arrange
        // GetOrderStatus implements IEventRequest<OrderStatusResponse>, so OrderStatusResponse is
        // a response registration reply type. The route-Kind detection path is covered by the saga
        // variant below. Both detection paths emit ReceivesReplyType via the same throw site.

        // act
        var responseRegistrationException = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection()
                .AddMessageBus()
                .AddRequestHandler<GetOrderStatusHandler>()
                .AddInMemory(t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("orders").Receives<OrderStatusResponse>();
                })
                .BuildRuntime());

        // assert
        Assert.Contains("reply type", responseRegistrationException.Message);
        Assert.Contains(nameof(OrderStatusResponse), responseRegistrationException.Message);
    }

    [Fact]
    public void Receives_Should_Throw_When_TypeIsReplyRouteKind()
    {
        // arrange
        // A saga with OnReply<StockInfoResult> registers an InboundRoute with Kind = Reply for that
        // type. Receives<StockInfoResult> must fail because reply routes are address-routed.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<OrderStockCheckSaga>();

        // act
        var routeKindException = Assert.Throws<InvalidOperationException>(() =>
            builder
                .AddInMemory(t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("orders").Receives<StockInfoResult>();
                })
                .BuildRuntime());

        // assert
        Assert.Contains("reply type", routeKindException.Message);
        Assert.Contains(nameof(StockInfoResult), routeKindException.Message);
    }

    [Fact]
    public void Receives_Should_NotThrow_When_TypeHasNonReplyRoute()
    {
        // arrange
        // OrderCreated has a normal Subscribe route via the event handler. Receives<OrderCreated>
        // must succeed and bind the handler to the explicit endpoint.

        // act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").Receives<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.NotEmpty(routes);
    }

    [Fact]
    public async Task AutoBind_Should_NotDeliver_When_QueueAutoBindFalseAndNoBindFrom()
    {
        // arrange
        // AutoBind(false) at queue scope removes the convention binding from the send topic into the
        // queue. Without a BindFrom to substitute an explicit binding, published messages never reach
        // the queue even though the type-owned topics exist.
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders")
                    .Receives<OrderCreated>()
                    .AutoBind(false);
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "NO-BIND" }, CancellationToken.None);

        // assert
        // A short wait confirms messages are not delivered: no queue binding was created.
        var received = await recorder.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.False(received);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task BindFrom_Should_Deliver_When_ExplicitBindDeclared()
    {
        // arrange
        // AutoBind(false) suppresses the convention queue binding. The queue-level BindFrom from
        // the send topic substitutes an explicit binding so published messages are still delivered.
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders")
                    .Receives<OrderCreated>()
                    .AutoBind(false)
                    .BindFrom(new Uri("topic:order-created"));
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "BIND-1" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(TimeSpan.FromSeconds(10)),
            "Handler did not receive the message via the explicit BindFrom binding.");

        var message = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("BIND-1", message.OrderId);
    }
}

public sealed class TestOrderConsumer : IConsumer<OrderCreated>
{
    public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
}

file sealed class StockCheckStarted;

file sealed class StockInfoResult;

file sealed class GetStockInfoRequest : IEventRequest<StockInfoResult>;

file sealed class StockCheckState : SagaStateBase;

file sealed class OrderStockCheckSaga : Saga<StockCheckState>
{
    protected override void Configure(ISagaDescriptor<StockCheckState> descriptor)
    {
        descriptor
            .Initially()
            .OnEvent<StockCheckStarted>()
            .StateFactory(_ => new StockCheckState())
            .Send((_, _) => new GetStockInfoRequest())
            .TransitionTo("Awaiting");

        descriptor.During("Awaiting").OnReply<StockInfoResult>().TransitionTo("Done");

        descriptor.Finally("Done");
    }
}
