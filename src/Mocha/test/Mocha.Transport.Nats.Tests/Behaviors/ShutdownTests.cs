using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public sealed class ShutdownTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_CancelHandlerAndReleaseMessage_When_DrainTimeoutPasses()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate();
        var bus = await BuildBusAsync(scope, gate, TimeSpan.FromSeconds(1), "shutdown");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // act
        // CancellationToken.None: the drain timeout alone has to bound the stop, as the host's
        // DisposeAsync path passes no token.
        await bus.Runtime.StopAsync(CancellationToken.None).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.True(gate.Cancelled);
        Assert.False(bus.Runtime.IsStarted);
        await bus.DisposeAsync();

        // Released rather than left to wait out AckWait (30 seconds): a fresh instance on the same
        // durable consumer gets it straight away.
        var counter = new InvocationCounter();
        await using var next = await BuildCountingBusAsync(scope, counter, "shutdown");
        Assert.True(
            await WaitForAtLeastAsync(counter, 1, TimeSpan.FromSeconds(10)),
            "The aborted message was not redelivered promptly.");
    }

    [Fact]
    public async Task StopAsync_Should_LetHandlerFinish_When_ItCompletesWithinTheDrainTimeout()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate { Duration = TimeSpan.FromMilliseconds(500) };
        var bus = await BuildBusAsync(scope, gate, TimeSpan.FromSeconds(10), "drain");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // act
        await bus.Runtime.StopAsync(CancellationToken.None).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.True(gate.Completed);
        Assert.False(gate.Cancelled);
        await bus.DisposeAsync();

        // Acknowledged, so nothing comes back to the next instance.
        var counter = new InvocationCounter();
        await using var next = await BuildCountingBusAsync(scope, counter, "drain");
        await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        Assert.Equal(0, counter.Count);
    }

    [Fact]
    public async Task StopAsync_Should_CancelHandler_When_TheCallersTokenFiresBeforeTheDrainTimeout()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate();
        await using var bus = await BuildBusAsync(scope, gate, TimeSpan.FromMinutes(5), "early");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // act
        await bus.Runtime.StopAsync(stop.Token).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.True(gate.Cancelled);
    }

    [Fact]
    public async Task StopAsync_Should_DrainEndpointsTogether_When_SeveralHaveHandlersRunning()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var first = new HandlerGate();
        var second = new HandlerGate();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddKeyedSingleton("first", first)
            .AddKeyedSingleton("second", second)
            .AddMessageBus()
            .AddEventHandler<FirstBlockingHandler>()
            .AddEventHandler<SecondBlockingHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.DrainTimeout(TimeSpan.FromSeconds(2));
                nats.Endpoint("first").Handler<FirstBlockingHandler>();
                nats.Endpoint("second").Handler<SecondBlockingHandler>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus);
        await Task.WhenAll(first.Started.Task, second.Started.Task)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var stopwatch = Stopwatch.StartNew();

        // act
        await bus.Runtime.StopAsync(CancellationToken.None).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        // One deadline for the transport: endpoints stopped one after another would take twice the
        // drain timeout.
        Assert.True(first.Cancelled);
        Assert.True(second.Cancelled);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3.5),
            $"Stopping took {stopwatch.Elapsed}, expected roughly one drain timeout.");
    }

    private async Task<TestBus> BuildBusAsync(
        JetStreamScope scope,
        HandlerGate gate,
        TimeSpan drainTimeout,
        string endpoint)
        => await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<BlockingHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.DrainTimeout(drainTimeout);
                nats.Endpoint(endpoint).Handler<BlockingHandler>();
            })
            .BuildTestBusAsync();

    private async Task<TestBus> BuildCountingBusAsync(
        JetStreamScope scope,
        InvocationCounter counter,
        string endpoint)
        => await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(counter)
            .AddMessageBus()
            .AddEventHandler<CountingHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint(endpoint).Handler<CountingHandler>();
            })
            .BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus)
    {
        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;
        await messageBus.PublishAsync(new ShutdownEvent(), TestContext.Current.CancellationToken);
    }

    private static async Task<bool> WaitForAtLeastAsync(
        InvocationCounter counter,
        int expected,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (counter.Count >= expected)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }

        return counter.Count >= expected;
    }

    public sealed class ShutdownEvent;

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimeSpan Duration { get; init; } = Timeout.InfiniteTimeSpan;

        public bool Cancelled { get; private set; }

        public bool Completed { get; private set; }

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            try
            {
                await Task.Delay(Duration, cancellationToken);
                Completed = true;
            }
            catch (OperationCanceledException)
            {
                Cancelled = true;
                throw;
            }
        }
    }

    public sealed class BlockingHandler(HandlerGate gate) : IEventHandler<ShutdownEvent>
    {
        public ValueTask HandleAsync(ShutdownEvent message, CancellationToken cancellationToken)
            => gate.RunAsync(cancellationToken);
    }

    public sealed class FirstBlockingHandler([FromKeyedServices("first")] HandlerGate gate)
        : IEventHandler<ShutdownEvent>
    {
        public ValueTask HandleAsync(ShutdownEvent message, CancellationToken cancellationToken)
            => gate.RunAsync(cancellationToken);
    }

    public sealed class SecondBlockingHandler([FromKeyedServices("second")] HandlerGate gate)
        : IEventHandler<ShutdownEvent>
    {
        public ValueTask HandleAsync(ShutdownEvent message, CancellationToken cancellationToken)
            => gate.RunAsync(cancellationToken);
    }

    public sealed class CountingHandler(InvocationCounter counter) : IEventHandler<ShutdownEvent>
    {
        public ValueTask HandleAsync(ShutdownEvent message, CancellationToken cancellationToken)
        {
            counter.Increment();
            return ValueTask.CompletedTask;
        }
    }
}
