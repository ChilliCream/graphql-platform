using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Example;

/// <summary>
/// Demonstrates Mocha.Testing patterns using an e-commerce order workflow.
///
/// The system under test has this message flow:
///   PlaceOrder -> OrderPlaced -> ProcessPayment -> PaymentCompleted -> ShipOrder -> OrderShipped
///
/// Each test below shows a different Mocha.Testing capability.
/// </summary>
public class OrderWorkflowTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // -----------------------------------------------------------------------
    // 1. Simple handler test — publish a command, assert on the resulting event.
    //    We register a sink for OrderPlaced because WaitForCompletionAsync
    //    waits for ALL dispatched messages to be consumed.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PlaceOrder_Should_PublishOrderPlaced_When_OrderIsValid()
    {
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-100", Amount = 49.99m }, CancellationToken.None);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // Assert
        Assert.True(result.Completed);
        var placed = result.ShouldHavePublished<OrderPlaced>();
        Assert.Equal("ORD-100", placed.OrderId);
        Assert.Equal(49.99m, placed.Amount);
    }

    // -----------------------------------------------------------------------
    // 2. Cascading flow — full order lifecycle from PlaceOrder to OrderShipped.
    //    The full pipeline is registered so every message has a consumer.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PlaceOrder_Should_CompleteFullLifecycle_When_AllHandlersRegistered()
    {
        await using var provider = await CreateFullPipelineAsync();

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act — one publish triggers the entire cascading workflow.
        await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-200", Amount = 99.99m }, CancellationToken.None);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // Assert — verify the final event was produced.
        Assert.True(result.Completed);
        Assert.Empty(result.Failed);

        var shipped = result.ShouldHavePublished<OrderShipped>();
        Assert.Equal("ORD-200", shipped.OrderId);
        Assert.Equal("TRK-ORD-200", shipped.TrackingNumber);
    }

    // -----------------------------------------------------------------------
    // 3. Delta tracking — step-by-step with multiple WaitForCompletionAsync calls.
    //    Each call returns only the messages since the previous call (delta).
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Workflow_Should_ReturnDelta_When_TrackedInSteps()
    {
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // Step 1: Place the first order.
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-300", Amount = 25.00m }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);

        // Step 1 assertions — delta contains only ORD-300.
        step1.ShouldHaveConsumed<PlaceOrder>();
        step1.ShouldHavePublished<OrderPlaced>();

        // Step 2: Place a second order.
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-301", Amount = 75.00m }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);

        // Step 2 assertions — only the second order's messages appear.
        var placed = step2.ShouldHavePublished<OrderPlaced>(o => o.OrderId == "ORD-301");
        Assert.Equal(75.00m, placed.Amount);

        // The cumulative tracker has messages from both steps.
        Assert.True(tracker.Dispatched.Count >= 4);
    }

    // -----------------------------------------------------------------------
    // 4. Diagnostics — ToDiagnosticString provides a human-readable summary.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PlaceOrder_Should_ProduceDiagnostics_When_SingleOrderProcessed()
    {
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-400", Amount = 10.00m }, CancellationToken.None);

        await tracker.WaitForCompletionAsync(Timeout);

        // ToDiagnosticString provides a human-readable summary of all tracked activity.
        var diagnostics = tracker.ToDiagnosticString();
        Assert.Contains("PlaceOrder", diagnostics);
        Assert.Contains("OrderPlaced", diagnostics);
        Assert.Contains("Dispatched", diagnostics);

        // The cumulative tracker provides structured access.
        Assert.Equal(2, tracker.Dispatched.Count);
        Assert.Equal(2, tracker.Consumed.Count);
        Assert.Empty(tracker.Failed);
    }

    // -----------------------------------------------------------------------
    // 5. Negative assertion — cancelled order should NOT trigger shipping.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PlaceOrder_Should_NotDispatchShipOrder_When_OrderIsCancelled()
    {
        await using var provider = await CreateFullPipelineAsync();

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act — publish a cancelled order.
        await bus.PublishAsync(
            new PlaceOrder
            {
                OrderId = "ORD-500",
                Amount = 50.00m,
                IsCancelled = true
            },
            CancellationToken.None);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // Assert — OrderCancelled was published, but no shipping occurred.
        Assert.True(result.Completed);
        result.ShouldHavePublished<OrderCancelled>();
        result.ShouldNotHaveDispatched<ShipOrder>();
        result.ShouldNotHaveDispatched<OrderShipped>();
    }

    // -----------------------------------------------------------------------
    // 6. WaitForConsumed — wait for a specific intermediate message.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task WaitForConsumed_Should_ReturnMessage_When_OrderPlacedIsConsumed()
    {
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedSink>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        await bus.PublishAsync(new PlaceOrder { OrderId = "ORD-600", Amount = 30.00m }, CancellationToken.None);

        // Wait specifically for OrderPlaced to be consumed (not just dispatched).
        var placed = await tracker.WaitForConsumed<OrderPlaced>(Timeout);

        // Assert
        Assert.Equal("ORD-600", placed.OrderId);
        Assert.Equal(30.00m, placed.Amount);
    }

    // -----------------------------------------------------------------------
    // 7. Failure handling — handler throws, appears in Failed.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProcessPayment_Should_AppearInFailed_When_HandlerThrows()
    {
        // Arrange — use the failing handler instead of the real one.
        await using var provider = await CreateBusAsync(b => b.AddRequestHandler<FailingPaymentHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-700", Amount = 100.00m }, CancellationToken.None);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // Assert — the message completed (it was handled, even if it failed).
        Assert.True(result.Completed);
        Assert.NotEmpty(result.Failed);

        var failed = result.Failed[0];
        Assert.IsType<InvalidOperationException>(failed.Exception);
        Assert.Contains("ORD-700", failed.Exception!.Message);
    }

    // -----------------------------------------------------------------------
    // Helpers — shared bus setup used by tests.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a bus with the full order pipeline registered.
    /// Sink handlers are only added for true leaf messages (no downstream handler).
    /// </summary>
    private static Task<ServiceProvider> CreateFullPipelineAsync()
    {
        return CreateBusAsync(b =>
        {
            b.AddEventHandler<PlaceOrderHandler>();
            b.AddEventHandler<OrderPlacedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
            b.AddEventHandler<PaymentCompletedHandler>();
            b.AddRequestHandler<ShipOrderHandler>();
            b.AddEventHandler<OrderShippedSink>();
            b.AddEventHandler<OrderCancelledSink>();
        });
    }

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();
        services.AddMessageTracking();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }
}
