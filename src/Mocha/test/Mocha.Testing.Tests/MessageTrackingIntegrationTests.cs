using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class MessageTrackingIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // --- Core Tracking Tests ---

    [Fact]
    public async Task WaitForCompletionAsync_Should_ReturnResult_When_SingleMessageConsumed()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        Assert.True(result.Completed);
        Assert.Single(result.Dispatched);
        Assert.Single(result.Consumed);
        Assert.Empty(result.Failed);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackCascadingMessages_When_HandlerPublishes()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<CascadingHandler>();
            b.AddEventHandler<ItemShippedHandler>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — OrderCreated triggers CascadingHandler which publishes ItemShipped
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — both messages were dispatched and consumed
        Assert.True(result.Completed);
        Assert.Equal(2, result.Dispatched.Count);
        Assert.Equal(2, result.Consumed.Count);
        result.ShouldHaveConsumed<OrderCreated>();
        result.ShouldHaveConsumed<ItemShipped>();
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_ReturnDelta_When_CalledMultipleTimes()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // step 1
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);

        // step 2
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);

        // assert — each result only has its own messages (delta)
        Assert.Single(step1.Dispatched);
        Assert.Single(step1.Consumed);
        var order1 = step1.ShouldHaveConsumed<OrderCreated>();
        Assert.Equal("ORD-1", order1.OrderId);

        Assert.Single(step2.Dispatched);
        Assert.Single(step2.Consumed);
        var order2 = step2.ShouldHaveConsumed<OrderCreated>();
        Assert.Equal("ORD-2", order2.OrderId);

        // cumulative tracker has both
        Assert.Equal(2, tracker.Dispatched.Count);
        Assert.Equal(2, tracker.Consumed.Count);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackFanOut_When_MultipleSubscribers()
    {
        // arrange — two handlers for the same event type
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<OrderCreatedHandler2>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "FAN-1" }, CancellationToken.None);

        // Wait for both handlers. Fan-out envelopes arrive staggered — the first
        // WaitForCompletionAsync may return after only one handler completes.
        // Loop until we see both or the overall timeout expires.
        var deadline = DateTime.UtcNow.Add(Timeout);
        while (DateTime.UtcNow < deadline && tracker.Consumed.Count < 2)
        {
            await tracker.WaitForCompletionAsync(Timeout);
        }

        // assert — cumulative tracker should see both handlers consumed
        Assert.True(tracker.Consumed.Count >= 2);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_ReportFailure_When_HandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<ThrowingHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        Assert.True(result.Completed);
        Assert.NotEmpty(result.Failed);

        var failed = result.Failed[0];
        Assert.IsType<InvalidOperationException>(failed.Exception);
    }

    [Fact]
    public async Task WaitForConsumed_Should_ReturnMessage_When_TypeMatches()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var order = await tracker.WaitForConsumed<OrderCreated>(Timeout);

        // assert
        Assert.Equal("ORD-1", order.OrderId);
    }

    // --- Assertion Tests ---

    [Fact]
    public async Task ShouldHavePublished_Should_ReturnMessage_When_MessageExists()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var order = result.ShouldHavePublished<OrderCreated>();
        Assert.Equal("ORD-1", order.OrderId);
    }

    [Fact]
    public async Task ShouldHavePublished_Should_Throw_When_MessageNotFound()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        Assert.Throws<MessageTrackingException>(() => result.ShouldHavePublished<ItemShipped>());
    }

    [Fact]
    public async Task ShouldHavePublished_Should_FilterByPredicate_When_PredicateProvided()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var order = result.ShouldHavePublished<OrderCreated>(m => m.OrderId == "ORD-2");
        Assert.Equal("ORD-2", order.OrderId);
    }

    [Fact]
    public async Task ShouldHaveSent_Should_ReturnMessage_When_SentMessageExists()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddRequestHandler<ProcessPaymentHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(
            new ProcessPayment { OrderId = "PAY-1", Amount = 49.99m },
            CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var payment = result.ShouldHaveSent<ProcessPayment>();
        Assert.Equal("PAY-1", payment.OrderId);
        Assert.Equal(49.99m, payment.Amount);
    }

    [Fact]
    public async Task ShouldHaveConsumed_Should_ReturnMessage_When_Consumed()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var order = result.ShouldHaveConsumed<OrderCreated>();
        Assert.Equal("ORD-1", order.OrderId);
    }

    [Fact]
    public async Task ShouldHaveConsumed_Should_FilterByPredicate_When_PredicateProvided()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var order = result.ShouldHaveConsumed<OrderCreated>(m => m.OrderId == "ORD-2");
        Assert.Equal("ORD-2", order.OrderId);
    }

    [Fact]
    public async Task ShouldNotHaveDispatched_Should_Succeed_When_TypeNotPresent()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — ItemShipped was never dispatched
        result.ShouldNotHaveDispatched<ItemShipped>();
    }

    [Fact]
    public async Task ShouldNotHaveDispatched_Should_Throw_When_TypePresent()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        Assert.Throws<MessageTrackingException>(() => result.ShouldNotHaveDispatched<OrderCreated>());
    }

    [Fact]
    public async Task ShouldHaveNoMessages_Should_Pass_When_Empty()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // act — no messages published, no wait needed
        var result = new MessageTrackingResult([], [], [], completed: true, TimeSpan.Zero);

        // assert
        result.ShouldHaveNoMessages();
    }

    [Fact]
    public async Task ShouldHaveNoMessages_Should_Throw_When_MessagesExist()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — result has messages, so ShouldHaveNoMessages should throw
        Assert.Throws<MessageTrackingException>(() => result.ShouldHaveNoMessages());
    }

    // --- Diagnostic Tests ---

    [Fact]
    public async Task Timeline_Should_ContainAllEvents_When_MessageProcessed()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var timeline = tracker.Timeline;
        Assert.True(timeline.Count >= 3);
        Assert.Contains(timeline, e => e.Kind == TrackedEventKind.Dispatched);
        Assert.Contains(timeline, e => e.Kind == TrackedEventKind.ConsumeCompleted);
    }

    [Fact]
    public async Task ToDiagnosticString_Should_ReturnFormattedOutput_When_MessagesTracked()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await tracker.WaitForCompletionAsync(Timeout);

        // assert
        var diagnostic = tracker.ToDiagnosticString();
        Assert.Contains("Dispatched", diagnostic);
        Assert.Contains("OrderCreated", diagnostic);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_ThrowWithDiagnostics_When_Timeout()
    {
        // arrange — use a handler that delays forever (relative to the short timeout)
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<SlowHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "SLOW-1" }, CancellationToken.None);

        // assert — very short timeout should cause MessageTrackingException
        var ex = await Assert.ThrowsAsync<MessageTrackingException>(
            () => tracker.WaitForCompletionAsync(TimeSpan.FromMilliseconds(100)));

        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ex.DiagnosticOutput);
    }

    // --- Helper ---

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

// Test message types
public sealed class OrderCreated
{
    public required string OrderId { get; init; }
}

public sealed class ItemShipped
{
    public required string TrackingNumber { get; init; }
}

public sealed class ProcessPayment
{
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
}

// Test handlers
public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated _, CancellationToken __) => default;
}

public sealed class OrderCreatedHandler2 : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated _, CancellationToken __) => default;
}

public sealed class ItemShippedHandler : IEventHandler<ItemShipped>
{
    public ValueTask HandleAsync(ItemShipped _, CancellationToken __) => default;
}

public sealed class ThrowingHandler : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Handler failed deliberately");
}

public sealed class CascadingHandler(IMessageBus bus) : IEventHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(
            new ItemShipped { TrackingNumber = $"TRK-{message.OrderId}" },
            cancellationToken);
    }
}

public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(ProcessPayment _, CancellationToken __) => default;
}

public sealed class SlowHandler : IEventHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
    }
}
