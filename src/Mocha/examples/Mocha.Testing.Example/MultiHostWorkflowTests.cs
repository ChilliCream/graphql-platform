using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Example;

/// <summary>
/// Demonstrates multi-host testing patterns using <see cref="MessageTracker"/>.
///
/// Pattern 1 — Shared tracker across independent buses:
///   Create a single <see cref="MessageTracker"/> and register it in multiple hosts.
///   Each host has its own InMemory transport and handlers. The shared tracker
///   aggregates events from all hosts into a single unified view.
///
/// Pattern 2 — Multi-transport single bus:
///   A single bus with multiple named InMemory transports simulates separate
///   microservices. Messages route across transport boundaries automatically.
///   This is the best approach when all hosts use InMemory transport, because
///   separate InMemory buses cannot exchange messages.
/// </summary>
public class MultiHostWorkflowTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // -----------------------------------------------------------------------
    // Pattern 1: Shared MessageTracker across independent buses
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SharedTracker_Should_SeeAllMessages_When_TwoBusesRegistered()
    {
        var tracker = new MessageTracker();

        await using var orderHost = await CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        }, tracker);

        await using var fulfillmentHost = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<ShipOrderHandler>();
            b.AddEventHandler<OrderShippedSink>();
        }, tracker);

        // Publish to each host independently.
        using (var scope = orderHost.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-M1", Amount = 150.00m }, CancellationToken.None);
        }

        using (var scope = fulfillmentHost.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.SendAsync(new ShipOrder { OrderId = "ORD-M1" }, CancellationToken.None);
        }

        // The shared tracker sees messages from both hosts.
        var result = await tracker.WaitForCompletionAsync(Timeout);

        Assert.True(result.Completed);
        result.ShouldHaveConsumed<PlaceOrder>();
        result.ShouldHaveConsumed<ShipOrder>();
        result.ShouldHavePublished<OrderPlaced>();
        result.ShouldHavePublished<OrderShipped>();
    }

    [Fact]
    public async Task SharedTracker_Should_TrackDelta_When_HostsPublishSequentially()
    {
        var tracker = new MessageTracker();

        await using var orderHost = await CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        }, tracker);

        await using var fulfillmentHost = await CreateBusAsync(b =>
        {
            b.AddRequestHandler<ShipOrderHandler>();
            b.AddEventHandler<OrderShippedSink>();
        }, tracker);

        // Step 1: Order host processes an order.
        using (var scope = orderHost.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-M2", Amount = 100.00m }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);
        step1.ShouldHavePublished<OrderPlaced>();
        step1.ShouldNotHaveDispatched<ShipOrder>();

        // Step 2: Fulfillment host ships the order — delta only shows step 2 messages.
        using (var scope = fulfillmentHost.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.SendAsync(new ShipOrder { OrderId = "ORD-M2" }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);
        var shipped = step2.ShouldHavePublished<OrderShipped>();
        Assert.Equal("ORD-M2", shipped.OrderId);
        Assert.Equal("TRK-ORD-M2", shipped.TrackingNumber);
    }

    // -----------------------------------------------------------------------
    // Pattern 1b: Attach to already-running hosts
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SharedTracker_Should_SeeMessages_When_AttachedAfterHostStarts()
    {
        // Hosts built without any tracking — the tracker attaches after the fact.
        await using var orderHost = await CreateBusWithoutTrackingAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        });

        await using var fulfillmentHost = await CreateBusWithoutTrackingAsync(b =>
        {
            b.AddRequestHandler<ShipOrderHandler>();
            b.AddEventHandler<OrderShippedSink>();
        });

        var tracker = new MessageTracker();
        using var subOrder = tracker.Attach(orderHost);
        using var subFulfillment = tracker.Attach(fulfillmentHost);

        using (var scope = orderHost.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-A1", Amount = 80.00m }, CancellationToken.None);
        }

        using (var scope = fulfillmentHost.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.SendAsync(new ShipOrder { OrderId = "ORD-A1" }, CancellationToken.None);
        }

        var result = await tracker.WaitForCompletionAsync(Timeout);

        Assert.True(result.Completed);
        result.ShouldHavePublished<OrderPlaced>();
        result.ShouldHavePublished<OrderShipped>();
    }

    // -----------------------------------------------------------------------
    // Pattern 2: Multi-transport single bus (cross-transport routing)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PlaceOrder_Should_CompleteFullLifecycle_When_HandlersSpanMultipleTransports()
    {
        var tracker = new MessageTracker();
        await using var provider = await CreateMultiTransportBusAsync(tracker);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-M3", Amount = 150.00m }, CancellationToken.None);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        Assert.True(result.Completed);
        Assert.Empty(result.Failed);

        var shipped = result.ShouldHavePublished<OrderShipped>();
        Assert.Equal("ORD-M3", shipped.OrderId);
        Assert.Equal("TRK-ORD-M3", shipped.TrackingNumber);
    }

    [Fact]
    public async Task PlaceOrder_Should_NotReachFulfillment_When_OrderIsCancelled()
    {
        var tracker = new MessageTracker();
        await using var provider = await CreateMultiTransportBusAsync(tracker);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(
            new PlaceOrder { OrderId = "ORD-M4", Amount = 50.00m, IsCancelled = true },
            CancellationToken.None);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        Assert.True(result.Completed);
        result.ShouldHavePublished<OrderCancelled>();
        result.ShouldNotHaveDispatched<ProcessPayment>();
        result.ShouldNotHaveDispatched<ShipOrder>();
    }

    [Fact]
    public async Task Workflow_Should_TrackDelta_When_MultipleOrdersSpanTransports()
    {
        var tracker = new MessageTracker();
        await using var provider = await CreateMultiTransportBusAsync(tracker);

        // Step 1: First order flows through both transports.
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-M5", Amount = 100.00m }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);
        Assert.True(step1.Completed);
        step1.ShouldHavePublished<OrderShipped>();

        // Step 2: Second order — delta contains only this order's messages.
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-M6", Amount = 200.00m }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);
        Assert.True(step2.Completed);

        var shipped = step2.ShouldHavePublished<OrderShipped>(s => s.OrderId == "ORD-M6");
        Assert.Equal("TRK-ORD-M6", shipped.TrackingNumber);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static async Task<ServiceProvider> CreateBusAsync(
        Action<IMessageBusHostBuilder> configure,
        MessageTracker tracker)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();
        services.AddMessageTracking(tracker);

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }

    private static async Task<ServiceProvider> CreateBusWithoutTrackingAsync(
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }

    private static async Task<ServiceProvider> CreateMultiTransportBusAsync(MessageTracker tracker)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        builder.AddEventHandler<PlaceOrderHandler>();
        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddRequestHandler<ProcessPaymentHandler>();
        builder.AddEventHandler<PaymentCompletedHandler>();
        builder.AddRequestHandler<ShipOrderHandler>();
        builder.AddEventHandler<OrderShippedSink>();
        builder.AddEventHandler<OrderCancelledSink>();

        builder.AddInMemory(transport =>
        {
            transport.Name("orders");
            transport.IsDefaultTransport();
        });

        builder.AddInMemory(transport =>
        {
            transport.Name("fulfillment");
            transport.BindHandlersExplicitly();
            transport.Handler<OrderPlacedHandler>();
            transport.Handler<ProcessPaymentHandler>();
            transport.Handler<PaymentCompletedHandler>();
            transport.Handler<ShipOrderHandler>();
            transport.Handler<OrderShippedSink>();
        });

        services.AddMessageTracking(tracker);

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }
}
