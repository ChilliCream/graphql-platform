using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class SagaRegistrationTests
{
    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    [Fact]
    public void Saga_Should_HaveCorrectName_When_Registered()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderSaga>()));

        // assert - saga name is "order-saga" (kebab-case, no suffix stripping for "Saga")
        var sagaConsumer = runtime.Consumers.FirstOrDefault(c => c.Name == "order-saga");
        Assert.NotNull(sagaConsumer);
    }

    [Fact]
    public void Saga_Should_RegisterEventMessageTypes_When_AddedToRuntime()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderSaga>()));

        // assert - saga transition event types should be registered
        var orderPlacedType = runtime.Messages.GetMessageType(typeof(OrderPlaced));
        Assert.NotNull(orderPlacedType);
    }

    [Fact]
    public void Saga_Should_CreateInboundRoutes_When_RegisteredWithEvents()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderSaga>()));

        // assert - inbound routes: 2 saga transitions + 1 reply consumer
        var routes = runtime.Router.InboundRoutes;
        Assert.Equal(3, routes.Count);
    }

    [Fact]
    public void Saga_Should_HaveReplyConsumerPresent_When_Registered()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderSaga>()));

        // assert
        var replyConsumer = runtime.Consumers.FirstOrDefault(c => c.Name == "Reply");
        Assert.NotNull(replyConsumer);
    }

    [Fact]
    public void MultipleSagas_Should_BeRegistered_When_AddedToRuntime()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.ConfigureMessageBus(h =>
            {
                ((MessageBusBuilder)h).AddSaga<OrderSaga>();
                ((MessageBusBuilder)h).AddSaga<ShippingSaga>();
            });
        });

        // assert - both saga consumers should exist by name
        Assert.NotNull(runtime.Consumers.FirstOrDefault(c => c.Name == "order-saga"));
        Assert.NotNull(runtime.Consumers.FirstOrDefault(c => c.Name == "shipping-saga"));
    }

    [Fact]
    public void SagaAndHandler_Should_Coexist_When_BothRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<StandaloneHandler>();
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderSaga>());
        });

        // assert - both handler and saga consumers found by name
        Assert.NotNull(runtime.Consumers.FirstOrDefault(c => c.Name == nameof(StandaloneHandler)));
        Assert.NotNull(runtime.Consumers.FirstOrDefault(c => c.Name == "order-saga"));
    }

    public sealed class OrderPlaced
    {
        public string OrderId { get; init; } = "";
        public decimal Total { get; init; }
    }

    public sealed class PaymentReceived
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderCompleted
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ShipmentDispatched
    {
        public string ShipmentId { get; init; } = "";
    }

    public sealed class ShipmentDelivered
    {
        public string ShipmentId { get; init; } = "";
    }

    public sealed class StandaloneEvent
    {
        public string Data { get; init; } = "";
    }

    public sealed class OrderSagaState : SagaStateBase
    {
        public string OrderId { get; set; } = "";
        public decimal Total { get; set; }
    }

    public sealed class ShippingSagaState : SagaStateBase
    {
        public string ShipmentId { get; set; } = "";
    }

    public sealed class OrderSaga : Saga<OrderSagaState>
    {
        protected override void Configure(ISagaDescriptor<OrderSagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderPlaced>()
                .StateFactory(e => new OrderSagaState { OrderId = e.OrderId, Total = e.Total })
                .TransitionTo("AwaitingPayment");

            descriptor
                .During("AwaitingPayment")
                .OnEvent<PaymentReceived>()
                .Then((_, _) => { })
                .TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    public sealed class ShippingSaga : Saga<ShippingSagaState>
    {
        protected override void Configure(ISagaDescriptor<ShippingSagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<ShipmentDispatched>()
                .StateFactory(e => new ShippingSagaState { ShipmentId = e.ShipmentId })
                .TransitionTo("InTransit");

            descriptor.During("InTransit").OnEvent<ShipmentDelivered>().Then((_, _) => { }).TransitionTo("Delivered");

            descriptor.Finally("Delivered");
        }
    }

    public sealed class StandaloneHandler : IEventHandler<StandaloneEvent>
    {
        public ValueTask HandleAsync(StandaloneEvent message, CancellationToken cancellationToken) => default;
    }
}
