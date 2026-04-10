using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Tests.IntegrationTests;

public class PublishSubscribeIntegrationTests : ConsumerIntegrationTestsBase
{
    [Fact]
    public async Task OrderCreatedHandler_Should_ReceiveEvent_When_EventPublished()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

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
    public async Task OrderCreatedHandler_Should_ReceiveMultipleEvents_When_MultipleEventsPublished()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

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
    public async Task AddHandler_Should_DetectEventHandler_When_CalledForEventHandler()
    {
        // arrange - use ConfigureMessageBus to call AddHandler<T> directly
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddScoped<OrderCreatedHandler>();
            b.ConfigureMessageBus(static h => h.AddHandler<OrderCreatedHandler>());
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout),
            "Handler was not called - AddHandler did not create SubscribeConsumer");

        var message = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-1", message.OrderId);
    }

    [Fact]
    public async Task MultipleHandlers_Should_Coexist_When_HandlingDifferentEvents()
    {
        // arrange
        var orderRecorder = new MessageRecorder();
        var shipmentRecorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddKeyedSingleton("order", orderRecorder);
            b.Services.AddKeyedSingleton("shipment", shipmentRecorder);
            b.AddEventHandler<OrderCreatedKeyedHandler>();
            b.AddEventHandler<ItemShippedKeyedHandler>();
        });

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
    public async Task EventAndRequestHandlers_Should_Coexist_When_BothPresent()
    {
        // arrange
        var eventRecorder = new MessageRecorder();
        var requestRecorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddKeyedSingleton("order", eventRecorder);
            b.Services.AddKeyedSingleton("request", requestRecorder);
            b.AddEventHandler<OrderCreatedKeyedHandler>();
            b.AddRequestHandler<GetOrderStatusKeyedHandler>();
        });

        // act - publish event
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        }

        // act - send request
        OrderStatusResponse response;
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            response = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-2" }, CancellationToken.None);
        }

        // assert
        Assert.True(await eventRecorder.WaitAsync(Timeout), "Event handler did not receive the event");
        Assert.True(await requestRecorder.WaitAsync(Timeout), "Request handler did not receive the request");

        Assert.Equal("Shipped", response.Status);
        Assert.Equal("ORD-2", response.OrderId);
    }

    [Fact]
    public async Task EventHandler_Should_NotCrashRuntime_When_ExceptionThrown()
    {
        // arrange
        var throwingRecorder = new MessageRecorder();
        var normalRecorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddKeyedSingleton("throwing", throwingRecorder);
            b.Services.AddKeyedSingleton("shipment", normalRecorder);
            b.AddEventHandler<ThrowingEventHandler>();
            b.AddEventHandler<ItemShippedKeyedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish event that triggers a throw
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);

        // wait a bit for the throwing handler to process
        await throwingRecorder.WaitAsync(TimeSpan.FromSeconds(2));

        // now publish a normal event
        await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-1" }, CancellationToken.None);

        // assert - the second handler still works
        Assert.True(
            await normalRecorder.WaitAsync(Timeout),
            "Normal handler did not receive event after a previous handler threw");
    }

    [Fact]
    public async Task Handler_Should_ResolveWithDIDependencies_When_Invoked()
    {
        // arrange
        var counter = new InvocationCounter();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(counter);
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<DependencyHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the event");

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverToAllSubscribers_When_MultipleSubscribersRegistered()
    {
        // arrange
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddKeyedSingleton("r1", recorder1);
            b.Services.AddKeyedSingleton("r2", recorder2);
            b.AddEventHandler<OrderCreatedKeyedHandler1>();
            b.AddEventHandler<OrderCreatedKeyedHandler2>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "MULTI-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder1.WaitAsync(Timeout));
        Assert.True(await recorder2.WaitAsync(Timeout));
        Assert.Single(recorder1.Messages);
        Assert.Single(recorder2.Messages);
    }

    [Fact]
    public async Task PublishAsync_Should_NotDeliverEvent_When_TokenCancelled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancel

        // act & assert - the framework may throw OperationCanceledException
        // or TaskCanceledException, or it may silently skip delivery.
        // We verify the event is not recorded by the handler either way.
        try
        {
            await bus.PublishAsync(new OrderCreated { OrderId = "cancelled" }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected path
        }

        // Task.Delay: negative wait - proves the cancelled event was NOT delivered
        await Task.Delay(200, default);
        Assert.DoesNotContain(recorder.Messages, m => m is OrderCreated oc && oc.OrderId == "cancelled");
    }

    [Fact]
    public async Task PublishAsync_Should_ProcessAllEvents_When_ConcurrentPublishCalled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<OrderCreatedHandler>();
        });

        // act - publish 10 events concurrently
        var tasks = Enumerable
            .Range(0, 10)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(new OrderCreated { OrderId = $"concurrent-{i}" }, CancellationToken.None);
            });

        await Task.WhenAll(tasks);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 10));
        Assert.Equal(10, recorder.Messages.Count);
    }
}
