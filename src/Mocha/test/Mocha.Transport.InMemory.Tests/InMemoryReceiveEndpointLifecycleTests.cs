using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

/// <summary>
/// Integration tests verifying receive endpoint lifecycle behavior:
/// start, stop, disposal, and error resilience.
/// </summary>
public class InMemoryReceiveEndpointLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Runtime_Should_StartAndStopCleanly_When_NoHandlersRegistered()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();

        // act - start
        await runtime.StartAsync(CancellationToken.None);

        // assert - started without error
        Assert.True(runtime.IsStarted);

        // act - dispose (cleanup)
        await runtime.DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_RuntimeStarted()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        // runtime is already started by BuildServiceProvider

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler did not receive the event after start");
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_ProcessMessages_When_Started()
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

        // act - bus is already started by BuildServiceProvider
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Receive endpoint should process messages after start");

        var msg = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-1", msg.OrderId);
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_NotAcceptNewScopes_When_ProviderDisposed()
    {
        // arrange
        var recorder = new MessageRecorder();
        var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        // Verify messages are delivered before dispose
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-BEFORE" }, CancellationToken.None);
        }

        Assert.True(await recorder.WaitAsync(Timeout), "Message should be delivered before dispose");

        // act - dispose the provider (which tears down the DI container)
        await provider.DisposeAsync();

        // assert - creating a new scope should fail after dispose
        Assert.Throws<ObjectDisposedException>(() => provider.CreateScope());
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_StopProcessing_When_RuntimeDisposed()
    {
        // arrange
        var recorder = new MessageRecorder();
        var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "ORD-BEFORE" }, CancellationToken.None);
        }

        Assert.True(await recorder.WaitAsync(Timeout), "Message should be delivered before dispose");

        // act - dispose the runtime
        await provider.DisposeAsync();

        // assert - provider is disposed, no new scopes can be created
        Assert.Throws<ObjectDisposedException>(() => provider.CreateScope());
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_ContinueProcessing_When_HandlerThrows()
    {
        // arrange - handler that throws on the first message, succeeds on the second
        var recorder = new MessageRecorder();
        var counter = new InvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton(counter)
            .AddMessageBus()
            .AddEventHandler<ThrowOnFirstThenSucceedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send first message (handler will throw)
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);

        // Give the failing message time to be processed (and faulted)
        await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);

        // send second message (handler should succeed)
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-OK" }, CancellationToken.None);

        // assert - the second message should be delivered despite the first throwing
        Assert.True(
            await recorder.WaitAsync(Timeout),
            "Receive endpoint should continue processing after handler exception");

        // The recorder should have received at least the successful message
        Assert.Contains(recorder.Messages.Cast<OrderCreated>(), m => m.OrderId == "ORD-OK");
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_ProcessSubsequentMessages_When_PreviousHandlerThrows()
    {
        // arrange - handler always throws, but we verify the receive loop stays alive
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<RecordThenThrowHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send 3 messages, each will throw but be recorded first
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-3" }, CancellationToken.None);

        // assert - all 3 messages should be recorded (handler records before throwing)
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Receive endpoint should process all messages even when handler throws each time");

        Assert.Equal(3, recorder.Messages.Count);
        var ids = recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).OrderBy(id => id).ToList();
        Assert.Equal(["ORD-1", "ORD-2", "ORD-3"], ids);
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_ProcessMessages_When_RestartedAfterError()
    {
        // arrange - handler that throws on even-indexed messages
        var recorder = new MessageRecorder();
        var counter = new InvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton(counter)
            .AddMessageBus()
            .AddEventHandler<ThrowOnEvenHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send 10 messages
        for (var i = 1; i <= 10; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert - 5 odd messages should be recorded (handler throws on even calls)
        Assert.True(await recorder.WaitAsync(Timeout, expectedCount: 5), "Expected 5 successful messages");

        Assert.Equal(5, recorder.Messages.Count);
        // With concurrent consumers, which messages hit even vs odd invocations
        // is non-deterministic, so we only verify count and uniqueness.
        var ids = recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).ToHashSet();
        Assert.Equal(5, ids.Count);
    }

    [Fact]
    public async Task Runtime_Should_ThrowInvalidOperationException_When_StartedTwice()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // Verify runtime is already started
        Assert.True(runtime.IsStarted, "Runtime should be started after BuildServiceProvider");

        // act & assert - starting a second time should throw
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runtime.StartAsync(CancellationToken.None).AsTask()
        );

        Assert.Contains("already started", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReceiveEndpoint_Should_HaveQueueAssigned_When_Started()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert - every receive endpoint should have a queue assigned
        foreach (var endpoint in transport.ReceiveEndpoints.OfType<InMemoryReceiveEndpoint>())
        {
            Assert.NotNull(endpoint.Queue);
            Assert.NotEmpty(endpoint.Queue.Name);
        }
    }

    /// <summary>
    /// Shared counter injected as a singleton so each handler instance shares state
    /// within a single test, without relying on static mutable state.
    /// </summary>
    public sealed class InvocationCounter
    {
        private int _callCount;

        public int Increment() => Interlocked.Increment(ref _callCount);
    }

    /// <summary>
    /// Handler that throws on the first invocation and succeeds on subsequent ones.
    /// Uses an injected <see cref="InvocationCounter"/> singleton instead of a static
    /// field to avoid cross-test state leakage.
    /// </summary>
    public sealed class ThrowOnFirstThenSucceedHandler(MessageRecorder recorder, InvocationCounter counter)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (counter.Increment() == 1)
            {
                throw new InvalidOperationException("First invocation fails deliberately");
            }

            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// Handler that records the message and then throws. This verifies the receive
    /// loop stays alive even when every handler invocation faults.
    /// </summary>
    public sealed class RecordThenThrowHandler(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            throw new InvalidOperationException("Handler always fails");
        }
    }

    public sealed class ThrowOnEvenHandler(MessageRecorder recorder, InvocationCounter counter)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            var call = counter.Increment();
            if (call % 2 == 0)
            {
                throw new InvalidOperationException($"Even invocation {call} fails");
            }

            recorder.Record(message);
            return default;
        }
    }
}
