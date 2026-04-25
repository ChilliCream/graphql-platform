using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Resources.Tests;

public class MochaMessageBusResourceSourceTests
{
    [Fact]
    public void Resources_Should_IncludeServiceResource_When_RuntimeBuilt()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        using var source = new MochaMessageBusResourceSource(runtime);

        // act
        var resources = source.Resources;

        // assert
        Assert.Contains(resources, r => r.Kind == "mocha.service");
    }

    [Fact]
    public void Resources_Should_IncludeMessageTypeAndHandlerResources_When_HandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        using var source = new MochaMessageBusResourceSource(runtime);

        // act
        var resources = source.Resources;

        // assert
        Assert.Contains(resources, r => r.Kind == "mocha.message_type");
        Assert.Contains(resources, r => r.Kind == "mocha.handler" && r.Id.Contains(nameof(OrderCreatedHandler), StringComparison.Ordinal));
    }

    [Fact]
    public void Resources_Should_IncludeTransportResource_When_InMemoryTransportRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        using var source = new MochaMessageBusResourceSource(runtime);

        // act
        var resources = source.Resources;

        // assert
        Assert.Contains(resources, r => r.Kind == "mocha.transport");
    }

    [Fact]
    public void GetChangeToken_Should_Fire_When_DispatchEndpointAdded()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        using var source = new MochaMessageBusResourceSource(runtime);

        // prime the change token before raising the event
        var fired = false;
        source.GetChangeToken().RegisterChangeCallback(_ => fired = true, null);

        // also prime resource snapshot to verify it refreshes
        var initialCount = source.Resources.Count;

        // act — simulate a lazy dispatch endpoint creation by dispatching a new send.
        var messageType = runtime.GetMessageType(typeof(LateBoundMessage));
        runtime.GetSendEndpoint(messageType);

        // assert — the source observed the new endpoint
        Assert.True(fired);
        Assert.True(source.Resources.Count > initialCount);
    }

    [Fact]
    public void Dispose_Should_UnsubscribeFromEndpointRouter()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var source = new MochaMessageBusResourceSource(runtime);

        // ensure token is wired so we can detect a fire
        var fired = false;
        source.GetChangeToken().RegisterChangeCallback(_ => fired = true, null);

        // act
        source.Dispose();
        var messageType = runtime.GetMessageType(typeof(LateBoundMessage));
        runtime.GetSendEndpoint(messageType);

        // assert — source is no longer reactive after disposal
        Assert.False(fired);
    }

    [Fact]
    public async Task DispatchEndpointAdded_Should_NotDeadlock_When_HandlerReentersRouter()
    {
        // arrange — a handler subscribed to DispatchEndpointAdded synchronously calls back into the
        // router. This guards against a future regression where the event is raised inside the
        // router's lock — that would deadlock here. The watchdog cancellation token failsafes the
        // assertion if the assumption breaks.
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        using var source = new MochaMessageBusResourceSource(runtime);

        // prime the change token so the source actually fires on endpoint added
        source.GetChangeToken();

        var reentrantCalls = 0;
        runtime.Endpoints.DispatchEndpointAdded += (_, _) =>
        {
            // call back into the router synchronously — should not deadlock
            _ = runtime.Endpoints.Endpoints; // read endpoints under the router's lock
            Interlocked.Increment(ref reentrantCalls);
        };

        // act — dispatch a brand new message type so the router creates a dispatch endpoint
        var task = Task.Run(() =>
        {
            var messageType = runtime.GetMessageType(typeof(LateBoundMessage));
            runtime.GetSendEndpoint(messageType);
        });

        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));

        // assert — completes without deadlocking, and the reentrant handler ran
        Assert.Same(task, completed);
        await task;
        Assert.True(reentrantCalls >= 1);
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

    public sealed class LateBoundMessage
    {
        public string Data { get; init; } = "";
    }
}
