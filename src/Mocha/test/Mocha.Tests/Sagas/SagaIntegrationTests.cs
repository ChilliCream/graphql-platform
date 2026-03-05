using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class SagaIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SagaInitialEvent_Should_CreateNewSagaInstance_When_Published()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderProcessingSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new OrderPlacedEvent
            {
                SagaId = sagaId,
                OrderId = "ORD-SAGA-1",
                Amount = 100m
            },
            CancellationToken.None);

        // assert — saga should transition to "AwaitingPayment"
        await WaitUntilAsync(() => storage.Load<OrderSagaState>("order-processing-saga", sagaId) is not null, s_timeout);
        var state = storage.Load<OrderSagaState>("order-processing-saga", sagaId)!;
        Assert.Equal("AwaitingPayment", state.State);
        Assert.Equal("ORD-SAGA-1", state.OrderId);
        Assert.Equal(100m, state.Amount);
    }

    [Fact]
    public async Task SagaMultiStep_Should_TransitionThroughAllStates_When_EventsPublishedInSequence()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderProcessingSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - step 1: place order (Initially -> AwaitingPayment)
        await bus.PublishAsync(
            new OrderPlacedEvent
            {
                SagaId = sagaId,
                OrderId = "ORD-MULTI-1",
                Amount = 250m
            },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderSagaState>("order-processing-saga", sagaId)?.State == "AwaitingPayment",
            s_timeout);
        var state = storage.Load<OrderSagaState>("order-processing-saga", sagaId)!;
        Assert.Equal("AwaitingPayment", state.State);

        // act - step 2: complete payment (AwaitingPayment -> AwaitingShipment)
        await bus.PublishAsync(
            new PaymentCompletedEvent { CorrelationId = sagaId, PaymentId = "PAY-001" },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderSagaState>("order-processing-saga", sagaId)?.State == "AwaitingShipment",
            s_timeout);
        state = storage.Load<OrderSagaState>("order-processing-saga", sagaId)!;
        Assert.Equal("AwaitingShipment", state.State);
        Assert.Equal("PAY-001", state.PaymentId);

        // act - step 3: ship order (AwaitingShipment -> Completed/Final, deletes saga)
        await bus.PublishAsync(
            new OrderShippedEvent { CorrelationId = sagaId, TrackingNumber = "TRACK-001" },
            CancellationToken.None);

        await WaitUntilAsync(() => storage.Load<OrderSagaState>("order-processing-saga", sagaId) is null, s_timeout);
        Assert.Null(storage.Load<OrderSagaState>("order-processing-saga", sagaId));
    }

    [Fact]
    public async Task SagaMultipleInstances_Should_BeIndependentByCorrelationId()
    {
        // arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderProcessingSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - create two saga instances
        await bus.PublishAsync(
            new OrderPlacedEvent
            {
                SagaId = id1,
                OrderId = "ORD-A",
                Amount = 100m
            },
            CancellationToken.None);
        await bus.PublishAsync(
            new OrderPlacedEvent
            {
                SagaId = id2,
                OrderId = "ORD-B",
                Amount = 200m
            },
            CancellationToken.None);

        await WaitUntilAsync(
            () =>
                storage.Load<OrderSagaState>("order-processing-saga", id1)?.State == "AwaitingPayment"
                && storage.Load<OrderSagaState>("order-processing-saga", id2)?.State == "AwaitingPayment",
            s_timeout);

        // act - advance only saga 1
        await bus.PublishAsync(
            new PaymentCompletedEvent { CorrelationId = id1, PaymentId = "PAY-A" },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderSagaState>("order-processing-saga", id1)?.State == "AwaitingShipment",
            s_timeout);

        // assert - saga 2 should still be in AwaitingPayment
        var state2 = storage.Load<OrderSagaState>("order-processing-saga", id2)!;
        Assert.Equal("AwaitingPayment", state2.State);
    }

    [Fact]
    public async Task SagaCoexistence_Should_WorkWithEventHandler_When_BothRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.ConfigureMessageBus(h =>
                ((MessageBusBuilder)h).AddSaga<SimpleSaga>());
            b.AddEventHandler<OrderPlacedEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new OrderPlacedEvent
            {
                SagaId = Guid.NewGuid(),
                OrderId = "ORD-COEXIST",
                Amount = 50m
            },
            CancellationToken.None);

        // assert - the handler should still receive the event
        Assert.True(await recorder.WaitAsync(s_timeout));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class OrderPlacedEventHandler(MessageRecorder recorder) : IEventHandler<OrderPlacedEvent>
    {
        public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    // Initial event: NOT ICorrelatable — saga creates new state via StateFactory.
    // The SagaId property is used by StateFactory to set a deterministic state.Id
    // so subsequent events can look up the saga by CorrelationId.
    public sealed class OrderPlacedEvent
    {
        public required Guid SagaId { get; init; }
        public required string OrderId { get; init; }
        public required decimal Amount { get; init; }
    }

    // Subsequent events: ICorrelatable so saga can load existing state by CorrelationId.
    public sealed class PaymentCompletedEvent : ICorrelatable
    {
        public required Guid CorrelationId { get; init; }
        public required string PaymentId { get; init; }

        Guid? ICorrelatable.CorrelationId => CorrelationId;
    }

    public sealed class OrderShippedEvent : ICorrelatable
    {
        public required Guid CorrelationId { get; init; }
        public required string TrackingNumber { get; init; }

        Guid? ICorrelatable.CorrelationId => CorrelationId;
    }

    public sealed class OrderSagaState : SagaStateBase
    {
        public string OrderId { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentId { get; set; } = "";
        public string TrackingNumber { get; set; } = "";
    }

    public sealed class OrderProcessingSaga : Saga<OrderSagaState>
    {
        protected override void Configure(ISagaDescriptor<OrderSagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderPlacedEvent>()
                .StateFactory(e => new OrderSagaState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .TransitionTo("AwaitingPayment");

            descriptor
                .During("AwaitingPayment")
                .OnEvent<PaymentCompletedEvent>()
                .Then((state, e) => state.PaymentId = e.PaymentId)
                .TransitionTo("AwaitingShipment");

            descriptor
                .During("AwaitingShipment")
                .OnEvent<OrderShippedEvent>()
                .Then((state, e) => state.TrackingNumber = e.TrackingNumber)
                .TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    public sealed class SimpleSaga : Saga<OrderSagaState>
    {
        protected override void Configure(ISagaDescriptor<OrderSagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderPlacedEvent>()
                .StateFactory(e => new OrderSagaState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}
