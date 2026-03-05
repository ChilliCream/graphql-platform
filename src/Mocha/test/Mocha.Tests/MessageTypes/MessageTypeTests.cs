using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class MessageTypeTests
{
    [Fact]
    public void EventHandler_Should_RegisterMessageType_When_AddingEventHandler()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.Equal(typeof(OrderCreated), messageType.RuntimeType);
    }

    [Fact]
    public void EventHandlerMessageType_Should_HaveUrnIdentity_When_Registered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.StartsWith("urn:message:", messageType.Identity);
    }

    [Fact]
    public void MessageType_Should_BeCompleted_When_Built()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.True(messageType.IsCompleted);
    }

    [Fact]
    public void MessageType_Should_BeRetrievable_When_QueryingByIdentityString()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);

        var retrieved = runtime.Messages.GetMessageType(messageType.Identity);
        Assert.NotNull(retrieved);
        Assert.Same(messageType, retrieved);
    }

    [Fact]
    public void MessageType_Should_BeRetrievable_When_QueryingByType()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.Equal(typeof(OrderCreated), messageType.RuntimeType);
    }

    [Fact]
    public void RequestTypeEnclosedMessageTypes_Should_NotContainOpenGenericIEventRequest_When_Registered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(ProcessPayment));
        Assert.NotNull(messageType);

        var enclosedRuntimeTypes = messageType.EnclosedMessageTypes.Select(mt => mt.RuntimeType).ToList();

        Assert.DoesNotContain(typeof(IEventRequest<>), enclosedRuntimeTypes);
    }

    [Fact]
    public void MessageTypeEnclosedMessageIdentities_Should_MatchEnclosedMessageTypes_When_Queried()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.Equal(messageType.EnclosedMessageTypes.Length, messageType.EnclosedMessageIdentities.Length);

        for (int i = 0; i < messageType.EnclosedMessageTypes.Length; i++)
        {
            Assert.Equal(messageType.EnclosedMessageTypes[i].Identity, messageType.EnclosedMessageIdentities[i]);
        }
    }

    [Fact]
    public void RequestResponseHandler_Should_RegisterResponseType_When_Added()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var responseType = runtime.Messages.GetMessageType(typeof(OrderStatusResponse));
        Assert.NotNull(responseType);
        Assert.Equal(typeof(OrderStatusResponse), responseType.RuntimeType);
        Assert.True(responseType.IsCompleted);
    }

    [Fact]
    public void RequestResponseHandler_Should_RegisterBothRequestAndResponseTypes_When_Added()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var requestType = runtime.Messages.GetMessageType(typeof(GetOrderStatus));
        var responseType = runtime.Messages.GetMessageType(typeof(OrderStatusResponse));

        Assert.NotNull(requestType);
        Assert.NotNull(responseType);
    }

    [Fact]
    public void MultipleHandlers_Should_RegisterIndependentMessageTypes_When_Added()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert
        Assert.NotNull(runtime.Messages.GetMessageType(typeof(OrderCreated)));
        Assert.NotNull(runtime.Messages.GetMessageType(typeof(ItemShipped)));
        Assert.NotNull(runtime.Messages.GetMessageType(typeof(ProcessPayment)));
    }

    [Fact]
    public void MessageTypeIsInterfaceFlag_Should_NotBeSet_When_TypeIsConcrete()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(messageType);
        Assert.False(messageType.IsInterface);
    }

    [Fact]
    public void EventHandler_Should_CreateOutboundRoutes_When_Added()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert - outbound routes should exist for dispatching
        Assert.NotEmpty(runtime.Router.OutboundRoutes);
    }

    [Fact]
    public void OutboundRoutes_Should_BeInitialized_When_Built()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var route in runtime.Router.OutboundRoutes)
        {
            Assert.True(route.IsInitialized);
        }
    }

    [Fact]
    public void OutboundRoutes_Should_HaveMessageType_When_Registered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var route in runtime.Router.OutboundRoutes)
        {
            Assert.NotNull(route.MessageType);
        }
    }

    [Fact]
    public void Router_Should_FindOutboundRoutes_When_QueryingByMessageType()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var outboundRoutes = runtime.Router.GetOutboundByMessageType(messageType);
        Assert.NotEmpty(outboundRoutes);
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
        public decimal Amount { get; init; }
    }

    public sealed class GetOrderStatus : IEventRequest<OrderStatusResponse>
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderStatusResponse
    {
        public string Status { get; init; } = "";
    }

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
            return new(new OrderStatusResponse { Status = "Shipped" });
        }
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
