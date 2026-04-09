using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class SharedTrackerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task WaitForCompletionAsync_Should_SeeBothHosts_When_TrackerRegisteredInBoth()
    {
        // arrange
        var tracker = new MessageTracker();
        await using var busA = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>(), tracker);
        await using var busB = await CreateBusAsync(
            b => b.AddEventHandler<ItemShippedHandler>(), tracker);

        // act — publish one message to each independent bus
        using (var scope = busA.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-S1" }, CancellationToken.None);
        }

        using (var scope = busB.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-S1" }, CancellationToken.None);
        }

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — both messages visible through the shared tracker
        Assert.True(result.Completed);
        Assert.Equal(2, result.Dispatched.Count);
        Assert.Equal(2, result.Consumed.Count);
        result.ShouldHaveConsumed<OrderCreated>();
        result.ShouldHaveConsumed<ItemShipped>();
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_ReturnDelta_When_CalledAcrossHosts()
    {
        // arrange
        var tracker = new MessageTracker();
        await using var busA = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>(), tracker);
        await using var busB = await CreateBusAsync(
            b => b.AddEventHandler<ItemShippedHandler>(), tracker);

        // step 1 — publish to bus A
        using (var scope = busA.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-D1" }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);

        // step 2 — publish to bus B
        using (var scope = busB.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-D1" }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);

        // assert — each step only contains its own messages
        Assert.Single(step1.Consumed);
        step1.ShouldHaveConsumed<OrderCreated>(m => m.OrderId == "ORD-D1");

        Assert.Single(step2.Consumed);
        step2.ShouldHaveConsumed<ItemShipped>(m => m.TrackingNumber == "TRK-D1");

        // cumulative tracker has both
        Assert.Equal(2, tracker.Consumed.Count);
    }

    [Fact]
    public async Task WaitForConsumed_Should_ReturnMessage_When_ConsumedOnAnyHost()
    {
        // arrange
        var tracker = new MessageTracker();
        await using var busA = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>(), tracker);
        await using var busB = await CreateBusAsync(
            b => b.AddEventHandler<ItemShippedHandler>(), tracker);

        // act — wait for a type consumed on bus B
        var waitTask = tracker.WaitForConsumed<ItemShipped>(Timeout);

        using (var scope = busB.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-W1" }, CancellationToken.None);
        }

        var item = await waitTask;

        // assert
        Assert.Equal("TRK-W1", item.TrackingNumber);
    }

    [Fact]
    public async Task IMessageTracker_Should_ResolveToSharedInstance_When_RegisteredInBothHosts()
    {
        // arrange
        var tracker = new MessageTracker();
        await using var busA = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>(), tracker);
        await using var busB = await CreateBusAsync(
            b => b.AddEventHandler<ItemShippedHandler>(), tracker);

        // act
        var resolvedA = busA.GetRequiredService<IMessageTracker>();
        var resolvedB = busB.GetRequiredService<IMessageTracker>();

        // assert — both hosts resolve to the same shared tracker
        Assert.Same(tracker, resolvedA);
        Assert.Same(tracker, resolvedB);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_Complete_When_BothHostsPublishConcurrently()
    {
        // arrange
        var tracker = new MessageTracker();
        await using var busA = await CreateBusAsync(
            b => b.AddEventHandler<OrderCreatedHandler>(), tracker);
        await using var busB = await CreateBusAsync(
            b => b.AddEventHandler<ItemShippedHandler>(), tracker);

        // act — concurrent publish from both hosts
        var taskA = Task.Run(async () =>
        {
            using var scope = busA.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-C" }, CancellationToken.None);
        });

        var taskB = Task.Run(async () =>
        {
            using var scope = busB.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-C" }, CancellationToken.None);
        });

        await Task.WhenAll(taskA, taskB);

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        Assert.True(result.Completed);
        Assert.Equal(2, result.Consumed.Count);
    }

    [Fact]
    public async Task Attach_Should_ReceiveEvents_When_HostAlreadyRunning()
    {
        // arrange — host built without any tracker registered
        await using var host = await CreateBusWithoutTrackingAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        // attach after the host is running
        var tracker = new MessageTracker();
        using var subscription = tracker.Attach(host);

        // act
        using (var scope = host.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-A1" }, CancellationToken.None);
        }

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert
        Assert.True(result.Completed);
        result.ShouldHaveConsumed<OrderCreated>(m => m.OrderId == "ORD-A1");
    }

    [Fact]
    public async Task Attach_Should_StopReceivingEvents_When_Disposed()
    {
        // arrange
        await using var host = await CreateBusWithoutTrackingAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());

        var tracker = new MessageTracker();
        var subscription = tracker.Attach(host);

        // publish while attached
        using (var scope = host.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-A2" }, CancellationToken.None);
        }

        await tracker.WaitForCompletionAsync(Timeout);
        Assert.Single(tracker.Consumed);

        // detach
        subscription.Dispose();

        // publish after detach — tracker should NOT see this
        using (var scope = host.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-A3" }, CancellationToken.None);
        }

        // give the bus a moment to process
        await Task.Delay(200);

        // assert — still only the first message
        Assert.Single(tracker.Consumed);
    }

    [Fact]
    public async Task Attach_Should_WorkAcrossMultipleHosts_When_AttachedToEach()
    {
        // arrange — two hosts, no tracking registered
        await using var hostA = await CreateBusWithoutTrackingAsync(
            b => b.AddEventHandler<OrderCreatedHandler>());
        await using var hostB = await CreateBusWithoutTrackingAsync(
            b => b.AddEventHandler<ItemShippedHandler>());

        var tracker = new MessageTracker();
        using var subA = tracker.Attach(hostA);
        using var subB = tracker.Attach(hostB);

        // act
        using (var scope = hostA.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-A4" }, CancellationToken.None);
        }

        using (var scope = hostB.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-A4" }, CancellationToken.None);
        }

        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — tracker sees both hosts
        Assert.True(result.Completed);
        Assert.Equal(2, result.Consumed.Count);
        result.ShouldHaveConsumed<OrderCreated>();
        result.ShouldHaveConsumed<ItemShipped>();
    }

    // --- Helpers ---

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

    // --- Helpers ---

    private static async Task<ServiceProvider> CreateBusAsync(
        Action<IMessageBusHostBuilder> configure,
        MessageTracker sharedTracker)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();
        services.AddMessageTracking(sharedTracker);

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }
}
