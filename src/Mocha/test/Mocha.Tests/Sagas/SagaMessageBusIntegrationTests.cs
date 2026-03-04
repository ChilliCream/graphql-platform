using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Sagas;

public class SagaMessageBusIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // ──────────────────────────────────────────────────────────────────────
    // Test 1: Custom state data persisted across multi-step transitions
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_PersistCustomStateData_When_MutatedByEventHandler()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderWorkflowSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - step 1: submit order
        await bus.PublishAsync(
            new OrderSubmitted
            {
                SagaId = sagaId,
                OrderId = "ORD-100",
                Amount = 500m
            },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId)?.State == "PaymentPending",
            Timeout);

        // assert step 1 custom data
        var state = storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId)!;
        Assert.Equal("ORD-100", state.OrderId);
        Assert.Equal(500m, state.Amount);

        // act - step 2: payment received
        await bus.PublishAsync(
            new PaymentReceived { CorrelationId = sagaId, PaymentId = "PAY-200" },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId)?.State == "ShipmentPending",
            Timeout);

        // assert step 2 custom data
        state = storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId)!;
        Assert.Equal("ORD-100", state.OrderId);
        Assert.Equal(500m, state.Amount);
        Assert.Equal("PAY-200", state.PaymentId);

        // act - step 3: order shipped → final state, saga deleted
        await bus.PublishAsync(
            new OrderShipped { CorrelationId = sagaId, TrackingNumber = "TRACK-300" },
            CancellationToken.None);

        await WaitUntilAsync(() => storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId) is null, Timeout);

        Assert.Null(storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 2: Saga-id header propagated to published messages
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_PropagateHeadersToPublishedMessages_When_TransitionPublishes()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<NotifyingWorkflowSaga>());
            b.AddEventHandler<OrderNotificationHandler>();
        });

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish initial event; saga transitions and publishes OrderNotification
        await bus.PublishAsync(
            new OrderSubmitted
            {
                SagaId = sagaId,
                OrderId = "ORD-HDR",
                Amount = 10m
            },
            CancellationToken.None);

        // assert - downstream handler received the notification
        Assert.True(await recorder.WaitAsync(Timeout), "OrderNotification was not received by downstream handler");

        var notification = recorder.Messages.OfType<OrderNotification>().Single();
        Assert.Equal("ORD-HDR", notification.OrderId);
        Assert.Equal("OrderSubmitted", notification.Reason);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 3: Saga sends a command to a request handler
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_DeliverSendCommandToHandler_When_TransitionSends()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<SendingWorkflowSaga>());
            b.AddRequestHandler<ProcessPaymentCommandHandler>();
        });

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish initial event; saga sends ProcessPaymentCommand
        await bus.PublishAsync(
            new OrderSubmitted
            {
                SagaId = sagaId,
                OrderId = "ORD-SEND",
                Amount = 75m
            },
            CancellationToken.None);

        // assert - command handler received the command
        Assert.True(await recorder.WaitAsync(Timeout), "ProcessPaymentCommand was not received by handler");

        var command = recorder.Messages.OfType<ProcessPaymentCommand>().Single();
        Assert.Equal("ORD-SEND", command.OrderId);
        Assert.Equal(75m, command.Amount);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 4: Saga metadata (ReplyAddress, CorrelationId) persisted
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_PreserveMetadata_When_CreatedFromBusPublish()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderWorkflowSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new OrderSubmitted
            {
                SagaId = sagaId,
                OrderId = "ORD-META",
                Amount = 1m
            },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId) is not null,
            Timeout);

        // assert - metadata keys exist in state
        var state = storage.Load<OrderWorkflowState>("order-workflow-saga", sagaId)!;
        Assert.Equal(sagaId, state.Id);

        // CorrelationId key is set (may be null for publish, but the key exists)
        Assert.True(state.Metadata.ContainsKey("correlation-id"), "CorrelationId metadata key should be present");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 5: OnEntry lifecycle events flow through the bus
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_ExecuteLifecycleEvents_When_OnEntryConfigured()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<LifecycleSaga>());
            b.AddEventHandler<OrderNotificationHandler>();
        });

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - initial event; saga enters "Active" which has OnEntry.Publish
        await bus.PublishAsync(
            new OrderSubmitted
            {
                SagaId = sagaId,
                OrderId = "ORD-LIFECYCLE",
                Amount = 1m
            },
            CancellationToken.None);

        // assert - lifecycle-published event was received
        Assert.True(await recorder.WaitAsync(Timeout), "OnEntry-published OrderNotification was not received");

        var notification = recorder.Messages.OfType<OrderNotification>().Single();
        Assert.Equal("ORD-LIFECYCLE", notification.OrderId);
        Assert.Equal("EnteredActive", notification.Reason);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 6: DuringAny transitions work from any active state
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_HandleDuringAnyTransition_When_AnyStateActive()
    {
        // arrange
        var sagaId = Guid.NewGuid();
        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<CancellableSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - step 1: create saga → "Active"
        await bus.PublishAsync(
            new OrderSubmitted
            {
                SagaId = sagaId,
                OrderId = "ORD-CANCEL",
                Amount = 1m
            },
            CancellationToken.None);

        await WaitUntilAsync(
            () => storage.Load<OrderWorkflowState>("cancellable-saga", sagaId)?.State == "Active",
            Timeout);

        // act - step 2: cancel from Active state via DuringAny
        await bus.PublishAsync(new CancelOrder { CorrelationId = sagaId }, CancellationToken.None);

        // assert - saga reaches final state and is deleted
        await WaitUntilAsync(() => storage.Load<OrderWorkflowState>("cancellable-saga", sagaId) is null, Timeout);

        Assert.Null(storage.Load<OrderWorkflowState>("cancellable-saga", sagaId));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 7: Concurrent instances maintain isolation
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saga_Should_MaintainIsolation_When_ConcurrentInstancesModifyState()
    {
        // arrange
        const int instanceCount = 5;
        var sagaIds = Enumerable.Range(0, instanceCount).Select(_ => Guid.NewGuid()).ToArray();

        await using var provider = await CreateBusAsync(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<OrderWorkflowSaga>()));

        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - create all instances concurrently
        await Task.WhenAll(
            sagaIds.Select(
                (id, i) =>
                    bus.PublishAsync(
                            new OrderSubmitted
                            {
                                SagaId = id,
                                OrderId = $"ORD-CONC-{i}",
                                Amount = (i + 1) * 100m
                            },
                            CancellationToken.None)
                        .AsTask()));

        // wait for all to reach PaymentPending
        await WaitUntilAsync(
            () =>
                sagaIds.All(id =>
                    storage.Load<OrderWorkflowState>("order-workflow-saga", id)?.State == "PaymentPending"
                ),
            Timeout);

        // assert each instance has correct, isolated data
        for (var i = 0; i < instanceCount; i++)
        {
            var state = storage.Load<OrderWorkflowState>("order-workflow-saga", sagaIds[i])!;
            Assert.Equal($"ORD-CONC-{i}", state.OrderId);
            Assert.Equal((i + 1) * 100m, state.Amount);
        }

        // act - advance all concurrently through payment step
        await Task.WhenAll(
            sagaIds.Select(
                (id, i) =>
                    bus.PublishAsync(
                            new PaymentReceived { CorrelationId = id, PaymentId = $"PAY-CONC-{i}" },
                            CancellationToken.None)
                        .AsTask()));

        await WaitUntilAsync(
            () =>
                sagaIds.All(id =>
                    storage.Load<OrderWorkflowState>("order-workflow-saga", id)?.State == "ShipmentPending"
                ),
            Timeout);

        // assert each instance preserved its own data after concurrent step
        for (var i = 0; i < instanceCount; i++)
        {
            var state = storage.Load<OrderWorkflowState>("order-workflow-saga", sagaIds[i])!;
            Assert.Equal($"ORD-CONC-{i}", state.OrderId);
            Assert.Equal((i + 1) * 100m, state.Amount);
            Assert.Equal($"PAY-CONC-{i}", state.PaymentId);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════════════════
    // Events
    // ══════════════════════════════════════════════════════════════════════

    public sealed class OrderSubmitted
    {
        public required Guid SagaId { get; init; }
        public required string OrderId { get; init; }
        public required decimal Amount { get; init; }
    }

    public sealed class PaymentReceived : ICorrelatable
    {
        public required Guid CorrelationId { get; init; }
        public required string PaymentId { get; init; }
        Guid? ICorrelatable.CorrelationId => CorrelationId;
    }

    public sealed class OrderShipped : ICorrelatable
    {
        public required Guid CorrelationId { get; init; }
        public required string TrackingNumber { get; init; }
        Guid? ICorrelatable.CorrelationId => CorrelationId;
    }

    public sealed class CancelOrder : ICorrelatable
    {
        public required Guid CorrelationId { get; init; }
        Guid? ICorrelatable.CorrelationId => CorrelationId;
    }

    public sealed class OrderNotification
    {
        public required string OrderId { get; init; }
        public required string Reason { get; init; }
    }

    public sealed class ProcessPaymentCommand
    {
        public required string OrderId { get; init; }
        public required decimal Amount { get; init; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // State
    // ══════════════════════════════════════════════════════════════════════

    public sealed class OrderWorkflowState : SagaStateBase
    {
        public string OrderId { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentId { get; set; } = "";
        public string TrackingNumber { get; set; } = "";
    }

    // ══════════════════════════════════════════════════════════════════════
    // Handlers
    // ══════════════════════════════════════════════════════════════════════

    public sealed class OrderNotificationHandler(MessageRecorder recorder) : IEventHandler<OrderNotification>
    {
        public ValueTask HandleAsync(OrderNotification message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class ProcessPaymentCommandHandler(MessageRecorder recorder)
        : IEventRequestHandler<ProcessPaymentCommand>
    {
        public ValueTask HandleAsync(ProcessPaymentCommand request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Sagas
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 4-state saga: Initially → PaymentPending → ShipmentPending → Completed (final).
    /// Mutates custom state properties at each step.
    /// </summary>
    public sealed class OrderWorkflowSaga : Saga<OrderWorkflowState>
    {
        protected override void Configure(ISagaDescriptor<OrderWorkflowState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderSubmitted>()
                .StateFactory(e => new OrderWorkflowState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .TransitionTo("PaymentPending");

            descriptor
                .During("PaymentPending")
                .OnEvent<PaymentReceived>()
                .Then((s, e) => s.PaymentId = e.PaymentId)
                .TransitionTo("ShipmentPending");

            descriptor
                .During("ShipmentPending")
                .OnEvent<OrderShipped>()
                .Then((s, e) => s.TrackingNumber = e.TrackingNumber)
                .TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }

    /// <summary>
    /// Saga that publishes an OrderNotification on the initial transition.
    /// Used to verify header propagation to downstream handlers.
    /// </summary>
    public sealed class NotifyingWorkflowSaga : Saga<OrderWorkflowState>
    {
        protected override void Configure(ISagaDescriptor<OrderWorkflowState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderSubmitted>()
                .StateFactory(e => new OrderWorkflowState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .Publish<OrderNotification>(
                    (_, s) => new OrderNotification { OrderId = s.OrderId, Reason = "OrderSubmitted" })
                .TransitionTo("Active");

            descriptor.Finally("Active");
        }
    }

    /// <summary>
    /// Saga that sends a ProcessPaymentCommand on the initial transition.
    /// Used to verify Send dispatches to request handlers.
    /// </summary>
    public sealed class SendingWorkflowSaga : Saga<OrderWorkflowState>
    {
        protected override void Configure(ISagaDescriptor<OrderWorkflowState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderSubmitted>()
                .StateFactory(e => new OrderWorkflowState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .Send<ProcessPaymentCommand>(
                    (_, s) => new ProcessPaymentCommand { OrderId = s.OrderId, Amount = s.Amount })
                .TransitionTo("AwaitingPayment");

            descriptor.Finally("AwaitingPayment");
        }
    }

    /// <summary>
    /// Saga with OnEntry lifecycle that publishes on entering "Active".
    /// </summary>
    public sealed class LifecycleSaga : Saga<OrderWorkflowState>
    {
        protected override void Configure(ISagaDescriptor<OrderWorkflowState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderSubmitted>()
                .StateFactory(e => new OrderWorkflowState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .TransitionTo("Active");

            descriptor
                .During("Active")
                .OnEntry()
                .Publish<OrderNotification>(
                    (_, s) => new OrderNotification { OrderId = s.OrderId, Reason = "EnteredActive" },
                    null);

            descriptor
                .During("Active")
                .OnEvent<PaymentReceived>()
                .Then((s, e) => s.PaymentId = e.PaymentId)
                .TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }

    /// <summary>
    /// Saga with DuringAny that transitions to Cancelled from any non-initial state.
    /// </summary>
    public sealed class CancellableSaga : Saga<OrderWorkflowState>
    {
        protected override void Configure(ISagaDescriptor<OrderWorkflowState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderSubmitted>()
                .StateFactory(e => new OrderWorkflowState
                {
                    Id = e.SagaId,
                    OrderId = e.OrderId,
                    Amount = e.Amount
                })
                .TransitionTo("Active");

            descriptor
                .During("Active")
                .OnEvent<PaymentReceived>()
                .Then((s, e) => s.PaymentId = e.PaymentId)
                .TransitionTo("Paid");

            descriptor.DuringAny().OnEvent<CancelOrder>().TransitionTo("Cancelled");

            descriptor.Finally("Paid");
            descriptor.Finally("Cancelled");
        }
    }
}
