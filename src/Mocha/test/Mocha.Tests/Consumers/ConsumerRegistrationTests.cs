using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Consumers;

public sealed class ConsumerRegistrationTests
{
    [Fact]
    public void AddEventHandler_Should_RegisterConsumerWithHandlerTypeName_When_EventHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        Assert.NotNull(consumer);
    }

    [Fact]
    public void AddEventHandler_Should_SetConsumerIdentityToHandlerType_When_EventHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        Assert.Equal(typeof(OrderCreatedHandler), consumer.Identity);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterConsumerWithHandlerTypeName_When_RequestHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));
        Assert.NotNull(consumer);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterConsumerWithHandlerTypeName_When_RequestResponseHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(GetOrderStatusHandler));
        Assert.NotNull(consumer);
    }

    [Fact]
    public void AddEventHandler_Should_RegisterSubscribeRoute_When_EventHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var routes = runtime.Router.GetInboundByConsumer(consumer);
        var route = Assert.Single(routes);
        Assert.Equal(InboundRouteKind.Subscribe, route.Kind);
    }

    [Fact]
    public void AddEventHandler_Should_RegisterCorrectMessageType_When_RouteCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(typeof(OrderCreated), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterSendRoute_When_RequestHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(InboundRouteKind.Send, route.Kind);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterCorrectMessageType_When_RouteCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(typeof(ProcessPayment), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterRequestRoute_When_RequestResponseHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(GetOrderStatusHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(InboundRouteKind.Request, route.Kind);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterCorrectMessageType_When_RequestResponseHandlerRouteCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(GetOrderStatusHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(typeof(GetOrderStatus), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterResponseType_When_RequestResponseHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert - the response type should be registered in the message type registry
        var responseType = runtime.Messages.GetMessageType(typeof(OrderStatusResponse));
        Assert.NotNull(responseType);
        Assert.Equal(typeof(OrderStatusResponse), responseType.RuntimeType);
    }

    [Fact]
    public void InboundRoutes_Should_BeInitialized_When_AfterBuild()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var route in runtime.Router.InboundRoutes)
        {
            Assert.True(route.IsInitialized);
        }
    }

    [Fact]
    public void InboundRoutes_Should_BeCompleted_When_AfterBuild()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var route in runtime.Router.InboundRoutes)
        {
            Assert.True(route.IsCompleted);
        }
    }

    [Fact]
    public void InboundRoutes_Should_HaveEndpoint_When_AfterBuild()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var route in runtime.Router.InboundRoutes)
        {
            Assert.NotNull(route.Endpoint);
        }
    }

    [Fact]
    public void Consumers_Should_IncludeReplyConsumer_When_RuntimeCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var replyConsumer = runtime.Consumers.FirstOrDefault(c => c.Name == "Reply");
        Assert.NotNull(replyConsumer);
    }

    [Fact]
    public void AddHandler_Should_CreateSubscribeRoute_When_EventHandlerConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.Services.AddScoped<OrderCreatedHandler>();
            b.ConfigureMessageBus(static h => h.AddHandler<OrderCreatedHandler>());
        });

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(InboundRouteKind.Subscribe, route.Kind);
        Assert.Equal(typeof(OrderCreated), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void AddHandler_Should_CreateSendRoute_When_RequestHandlerConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.Services.AddScoped<ProcessPaymentHandler>();
            b.ConfigureMessageBus(static h => h.AddHandler<ProcessPaymentHandler>());
        });

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(InboundRouteKind.Send, route.Kind);
        Assert.Equal(typeof(ProcessPayment), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void AddHandler_Should_CreateRequestRoute_When_RequestResponseHandlerConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.Services.AddScoped<GetOrderStatusHandler>();
            b.ConfigureMessageBus(static h => h.AddHandler<GetOrderStatusHandler>());
        });

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(GetOrderStatusHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(InboundRouteKind.Request, route.Kind);
        Assert.Equal(typeof(GetOrderStatus), route.MessageType!.RuntimeType);
    }

    [Fact]
    public void MultipleEventHandlers_Should_RegisterIndependentRoutes_When_Added()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
        });

        // assert
        var orderConsumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var shipConsumer = runtime.Consumers.First(c => c.Name == nameof(ItemShippedHandler));

        var orderRoute = Assert.Single(runtime.Router.GetInboundByConsumer(orderConsumer));
        var shipRoute = Assert.Single(runtime.Router.GetInboundByConsumer(shipConsumer));

        Assert.Equal(typeof(OrderCreated), orderRoute.MessageType!.RuntimeType);
        Assert.Equal(typeof(ItemShipped), shipRoute.MessageType!.RuntimeType);
    }

    [Fact]
    public void Consumers_Should_CoexistWithMixedHandlerTypes_When_Added()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
            b.AddRequestHandler<GetOrderStatusHandler>();
        });

        // assert - 3 handlers + ReplyConsumer
        Assert.Equal(4, runtime.Consumers.Count);

        var kinds = runtime
            .Router.InboundRoutes.Where(r => r.Kind != InboundRouteKind.Reply)
            .Select(r => r.Kind)
            .OrderBy(k => k)
            .ToList();

        Assert.Contains(InboundRouteKind.Subscribe, kinds);
        Assert.Contains(InboundRouteKind.Send, kinds);
        Assert.Contains(InboundRouteKind.Request, kinds);
    }

    [Fact]
    public void AddEventHandler_Should_RegisterEventMessageType_When_EventHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType.IsCompleted);
    }

    [Fact]
    public void AddRequestHandler_Should_RegisterRequestMessageType_When_RequestHandlerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(ProcessPayment));
        Assert.NotNull(messageType);
        Assert.True(messageType.IsCompleted);
    }

    [Fact]
    public void Router_Should_FindRouteByMessageType_When_MessageTypeQueried()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var routes = runtime.Router.GetInboundByMessageType(messageType);
        Assert.Single(routes);
    }

    [Fact]
    public void Router_Should_FindRouteByConsumer_When_ConsumerQueried()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
        });

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var routes = runtime.Router.GetInboundByConsumer(consumer);
        Assert.Single(routes);
        Assert.Equal(typeof(OrderCreated), routes.First().MessageType!.RuntimeType);
    }

    [Fact]
    public void Consumer_Should_AllowNameOverride_When_DescriptorConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.ConfigureMessageBus(h =>
                ((MessageBusBuilder)h).AddHandler<OrderCreatedHandler>(d => d.Name("CustomConsumer"))
            );
            b.Services.AddScoped<OrderCreatedHandler>();
        });

        // assert
        var consumer = runtime.Consumers.FirstOrDefault(c => c.Name == "CustomConsumer");
        Assert.NotNull(consumer);
        Assert.Equal(typeof(OrderCreatedHandler), consumer.Identity);
    }

    [Fact]
    public void AddConsumer_Should_RegisterConsumerWithTypeName_When_ConsumerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddConsumer<OrderCreatedConsumer>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedConsumer));
        Assert.NotNull(consumer);
    }

    [Fact]
    public void AddConsumer_Should_SetConsumerIdentityToConsumerType_When_ConsumerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddConsumer<OrderCreatedConsumer>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedConsumer));
        Assert.Equal(typeof(OrderCreatedConsumer), consumer.Identity);
    }

    [Fact]
    public void AddConsumer_Should_RegisterSubscribeRoute_When_ConsumerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddConsumer<OrderCreatedConsumer>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedConsumer));
        var routes = runtime.Router.GetInboundByConsumer(consumer);
        var route = Assert.Single(routes);
        Assert.Equal(InboundRouteKind.Subscribe, route.Kind);
    }

    [Fact]
    public void AddConsumer_Should_RegisterCorrectMessageType_When_ConsumerAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddConsumer<OrderCreatedConsumer>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedConsumer));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(typeof(OrderCreated), route.MessageType!.RuntimeType);
    }

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ItemShipped
    {
        public string TrackingNumber { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public string OrderId { get; init; } = "";
        public decimal Amount { get; init; }
    }

    public sealed class GetOrderStatus : IEventRequest<OrderStatusResponse>
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderStatusResponse
    {
        public string OrderId { get; init; } = "";
        public string Status { get; init; } = "";
    }

    // --- Handlers ---

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ItemShippedHandler : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }

    public sealed class GetOrderStatusHandler : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
        {
            return new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
        }
    }

    public sealed class OrderCreatedConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
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
}
