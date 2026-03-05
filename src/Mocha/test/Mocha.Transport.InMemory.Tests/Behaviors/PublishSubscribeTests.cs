using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class PublishSubscribeTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_DeliverToHandler_When_SingleHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-1", order.OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_FanOutToAllHandlers_When_MultipleHandlersRegistered()
    {
        // arrange
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("r1", recorder1)
            .AddKeyedSingleton("r2", recorder2)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedKeyedHandler1>()
            .AddEventHandler<OrderCreatedKeyedHandler2>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder1.WaitAsync(Timeout), "First handler did not receive the event");
        Assert.True(await recorder2.WaitAsync(Timeout), "Second handler did not receive the event");

        Assert.Single(recorder1.Messages);
        Assert.Single(recorder2.Messages);
    }

    [Fact]
    public async Task PublishAsync_Should_CompleteSilently_When_NoHandlerRegistered()
    {
        // arrange
        await using var provider = await new ServiceCollection().AddMessageBus().AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - should not throw
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert - completing without exception is the contract; no handler means silent discard
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToCorrectHandler_When_DifferentEventTypes()
    {
        // arrange
        var orderRecorder = new MessageRecorder();
        var shipmentRecorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("order", orderRecorder)
            .AddKeyedSingleton("shipment", shipmentRecorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedKeyedHandler>()
            .AddEventHandler<ItemShippedKeyedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-1" }, CancellationToken.None);

        // assert
        Assert.True(await orderRecorder.WaitAsync(Timeout), "OrderCreated handler did not receive the event");
        Assert.True(await shipmentRecorder.WaitAsync(Timeout), "ItemShipped handler did not receive the event");

        Assert.IsType<OrderCreated>(Assert.Single(orderRecorder.Messages));
        Assert.IsType<ItemShipped>(Assert.Single(shipmentRecorder.Messages));
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAll_When_MultipleEventsSequential()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-3" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Handler did not receive all 3 events within timeout");

        Assert.Equal(3, recorder.Messages.Count);

        var ids = recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).OrderBy(id => id).ToList();

        Assert.Equal(["ORD-1", "ORD-2", "ORD-3"], ids);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAll_When_RapidFire()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        const int messageCount = 50;

        // act - rapid-fire publish
        for (var i = 0; i < messageCount; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: messageCount),
            $"Handler did not receive all {messageCount} events within timeout");

        Assert.Equal(messageCount, recorder.Messages.Count);

        var ids = recorder
            .Messages.Cast<OrderCreated>()
            .Select(m => m.OrderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        // Verify all unique IDs present
        Assert.Equal(messageCount, ids.Distinct().Count());
    }

    public sealed class ItemShipped
    {
        public required string TrackingNumber { get; init; }
    }

    public sealed class OrderCreatedKeyedHandler([FromKeyedServices("order")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class ItemShippedKeyedHandler([FromKeyedServices("shipment")] MessageRecorder recorder)
        : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class OrderCreatedKeyedHandler1([FromKeyedServices("r1")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class OrderCreatedKeyedHandler2([FromKeyedServices("r2")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
