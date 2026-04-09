using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public sealed class RedeliveryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Redelivery_Should_ScheduleRedelivery_When_HandlerFails()
    {
        // arrange
        var counter = new InvocationCounter();
        var recorder = new MessageRecorder();

        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>().Redeliver(
                [
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromMilliseconds(1)
                ]);
            })
            .AddEventHandler<ThrowThenSucceedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-REDELIVER" }, CancellationToken.None);

        // assert - handler fails on first delivery, succeeds on redelivery
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler did not record the message after redelivery");

        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_SkipRedelivery_When_ExceptionIsIgnored()
    {
        // arrange
        var counter = new InvocationCounter();

        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>().Redeliver([TimeSpan.FromMilliseconds(1)]);
                p.On<InvalidOperationException>().DeadLetter();
            })
            .AddEventHandler<ThrowInvalidOperationHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-IGNORED" }, CancellationToken.None);

        // assert - only 1 invocation, exception propagates without redelivery
        await Task.Delay(500);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_PassThrough_When_Disabled()
    {
        // arrange — no exception policy configured, so retry and redelivery are both no-ops
        var counter = new InvocationCounter();

        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-DISABLED" }, CancellationToken.None);

        // assert - no exception policy: only 1 invocation, no retry or redelivery
        await Task.Delay(500);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_PropagateToFault_When_AllAttemptsExhausted()
    {
        // arrange - 2 redelivery intervals = 2 redeliveries max, 3 total attempts
        var counter = new InvocationCounter();

        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>().Redeliver(
                [
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromMilliseconds(1)
                ]);
            })
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-EXHAUST" }, CancellationToken.None);

        // assert - 1 original + 2 redeliveries = 3 total invocations
        Assert.True(
            await counter.WaitForCountAsync(3, s_timeout),
            $"Expected 3 invocations (1 original + 2 redeliveries), but got {counter.Count}");
    }

    [Fact]
    public async Task Redelivery_Should_UseEndpointOverride_When_EndpointConfigured()
    {
        // arrange - bus-level: redeliver once, but the transport overrides to discard
        var counter = new InvocationCounter();
        var builder = new ServiceCollection()
            .AddSingleton(counter)
            .AddScoped<AlwaysThrowingHandler>()
            .AddMessageBus()
            .AddResilience(p =>
                p.On<Exception>().Redeliver([TimeSpan.FromMilliseconds(1)]));

        // Override at transport level to discard all exceptions.
        builder.ConfigureMessageBus(b => b.AddHandler<AlwaysThrowingHandler>());

        await using var provider = await builder
            .AddInMemory(t => t.AddResilience(p =>
                p.On<Exception>().DeadLetter()))
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-OVERRIDE" }, CancellationToken.None);

        // assert - redelivery disabled at transport level via DeadLetter: only 1 invocation
        await Task.Delay(500);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_UseDefaults_When_ParameterlessAddResilience()
    {
        // arrange - defaults: 3 redelivery intervals from RedeliveryPolicyDefaults
        var counter = new InvocationCounter();

        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddResilience()
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULT" }, CancellationToken.None);

        // assert - default is 3 retries + 3 redeliveries: 1 original + 3 retries = 4 on first delivery,
        // then 3 redeliveries each with 1 original + 3 retries = 4, total = 4 + 3*4 = 16
        // Wait for at least the first 4 (initial delivery with retries)
        Assert.True(
            await counter.WaitForCountAsync(4, s_timeout),
            $"Expected at least 4 invocations (1 original + 3 default retries), but got {counter.Count}");
    }

    // ============================================================
    // Test Helpers
    // ============================================================

    private sealed class InvocationCounter
    {
        private int _count;
        private readonly SemaphoreSlim _semaphore = new(0);

        public int Count => _count;

        public void Increment()
        {
            Interlocked.Increment(ref _count);
            _semaphore.Release();
        }

        public async Task<bool> WaitForCountAsync(int targetCount, TimeSpan timeout)
        {
            for (var i = 0; i < targetCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // ============================================================
    // Test Handlers
    // ============================================================

    /// <summary>
    /// Throws on the first invocation, succeeds on subsequent invocations.
    /// </summary>
    private sealed class ThrowThenSucceedHandler(InvocationCounter counter, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            var invocation = counter.Count;
            counter.Increment();

            if (invocation == 0)
            {
                throw new InvalidOperationException("Transient failure");
            }

            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// Always throws an InvalidOperationException.
    /// </summary>
    private sealed class AlwaysThrowingHandler(InvocationCounter counter) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            counter.Increment();
            throw new InvalidOperationException("Always fails");
        }
    }

    /// <summary>
    /// Always throws an InvalidOperationException (for the Ignore test).
    /// </summary>
    private sealed class ThrowInvalidOperationHandler(InvocationCounter counter) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            counter.Increment();
            throw new InvalidOperationException("Should be ignored");
        }
    }
}
