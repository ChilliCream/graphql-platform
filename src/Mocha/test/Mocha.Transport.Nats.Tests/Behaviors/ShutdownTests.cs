using System.Diagnostics;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public sealed class ShutdownTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task StopAsync_Should_CancelTheHandler_When_TheDrainTimeoutPasses()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate();
        await using var bus = await BuildBusAsync(scope, gate, TimeSpan.FromSeconds(1), "shutdown");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        await StopAsync(bus, CancellationToken.None);

        // assert
        Assert.True(gate.Cancelled);
        Assert.False(bus.Runtime.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_ReleaseTheAbortedMessage_When_TheDrainTimeoutPasses()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate();
        var bus = await BuildBusAsync(scope, gate, TimeSpan.FromSeconds(1), "release");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var redelivery = new RedeliveryProbe();

        // act
        await StopAsync(bus, CancellationToken.None);
        await bus.DisposeAsync();
        await using var next = await BuildProbeBusAsync(scope, redelivery, "release");
        var redelivered = await redelivery.WaitAsync(s_timeout);

        // assert
        Assert.True(redelivered);
    }

    [Fact]
    public async Task StopAsync_Should_AcknowledgeTheMessage_When_TheHandlerFinishesWithinTheDrainTimeout()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate { Duration = TimeSpan.FromMilliseconds(500) };
        await using var bus = await BuildBusAsync(scope, gate, TimeSpan.FromSeconds(10), "drain");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        await StopAsync(bus, CancellationToken.None);
        var consumer = await WaitForAcknowledgedAsync(bus, "drain");

        // assert
        new
        {
            gate.Completed,
            gate.Cancelled,
            Delivered = consumer.Delivered.StreamSeq,
            Acknowledged = consumer.AckFloor.StreamSeq,
            consumer.NumAckPending
        }.MatchInlineSnapshot(
            """
            {
              "Completed": true,
              "Cancelled": false,
              "Delivered": 1,
              "Acknowledged": 1,
              "NumAckPending": 0
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_CancelTheHandler_When_TheCallersTokenFiresBeforeTheDrainTimeout()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var gate = new HandlerGate();
        await using var bus = await BuildBusAsync(scope, gate, TimeSpan.FromMinutes(5), "early");
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // act
        await StopAsync(bus, stop.Token);

        // assert
        Assert.True(gate.Cancelled);
    }

    [Fact]
    public async Task StopAsync_Should_ShareOneDeadline_When_SeveralEndpointsHaveHandlersRunning()
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
                nats.DrainTimeout(TimeSpan.FromSeconds(3));
                nats.Endpoint("first").Handler<FirstBlockingHandler>();
                nats.Endpoint("second").Handler<SecondBlockingHandler>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus);
        await Task.WhenAll(first.Started.Task, second.Started.Task)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var stopwatch = Stopwatch.StartNew();

        // act
        await StopAsync(bus, CancellationToken.None);

        // assert
        Assert.True(first.Cancelled);
        Assert.True(second.Cancelled);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
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

    private async Task<TestBus> BuildProbeBusAsync(
        JetStreamScope scope,
        RedeliveryProbe probe,
        string endpoint)
        => await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(probe)
            .AddMessageBus()
            .AddEventHandler<ProbeHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint(endpoint).Handler<ProbeHandler>();
            })
            .BuildTestBusAsync();

    private async Task<ConsumerInfo> WaitForAcknowledgedAsync(TestBus bus, string endpoint)
    {
        var consumer = bus.Topology.Consumers.Single(c => c.Name == endpoint);
        var deadline = DateTimeOffset.UtcNow + s_timeout;

        while (true)
        {
            var info = (await fixture.JetStream.GetConsumerAsync(
                consumer.StreamName!,
                consumer.Name,
                TestContext.Current.CancellationToken)).Info;

            if (info.NumAckPending == 0 || DateTimeOffset.UtcNow > deadline)
            {
                return info;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }
    }

    private static async Task StopAsync(TestBus bus, CancellationToken cancellationToken)
        => await bus.Runtime.StopAsync(cancellationToken).AsTask()
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

    private static async Task PublishAsync(TestBus bus)
    {
        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;
        await messageBus.PublishAsync(new ShutdownEvent(), TestContext.Current.CancellationToken);
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

    public sealed class RedeliveryProbe
    {
        private readonly TaskCompletionSource _received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Received() => _received.TrySetResult();

        public async Task<bool> WaitAsync(TimeSpan timeout)
        {
            try
            {
                await _received.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
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

    public sealed class ProbeHandler(RedeliveryProbe probe) : IEventHandler<ShutdownEvent>
    {
        public ValueTask HandleAsync(ShutdownEvent message, CancellationToken cancellationToken)
        {
            probe.Received();
            return ValueTask.CompletedTask;
        }
    }
}
