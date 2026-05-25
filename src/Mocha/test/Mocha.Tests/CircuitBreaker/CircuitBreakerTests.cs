using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class CircuitBreakerMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ClosedCircuit_Should_AllowMessageToFlow_When_HandlerSucceeds()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<AlwaysSucceedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Data = "ok-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task ClosedCircuit_Should_AllowMultipleMessagesToFlow_When_AllHandlersSucceed()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<AlwaysSucceedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Data = "ok-1" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Data = "ok-2" }, CancellationToken.None);
        await bus.PublishAsync(new TestEvent { Data = "ok-3" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout, expectedCount: 3));
        Assert.Equal(3, recorder.Messages.Count);
    }

    [Fact]
    public async Task MessagingRuntime_Should_Survive_When_HandlerRepeatedlyFails()
    {
        // arrange
        var counter = new InvocationCounter();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(counter);
            b.AddEventHandler<AlwaysThrowingHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send many failing messages
        for (var i = 0; i < 10; i++)
        {
            await bus.PublishAsync(new TestEvent { Data = $"fail-{i}" }, CancellationToken.None);
        }

        await Task.Delay(2000, default); // Wait for async processing

        // assert - runtime still alive
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task CircuitBreaker_Should_AllowMessagesToFlow_When_CustomConfiguredAndHandlerSucceeds()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddCircuitBreaker(o =>
            {
                o.FailureRatio = 0.5;
                o.MinimumThroughput = 5;
                o.BreakDuration = TimeSpan.FromSeconds(2);
                o.SamplingDuration = TimeSpan.FromSeconds(5);
            });
            b.AddEventHandler<AlwaysSucceedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Data = "custom-1" }, CancellationToken.None);

        // assert - messages still flow when circuit is closed
        Assert.True(await recorder.WaitAsync(s_timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task CircuitBreaker_Should_AllowMessagesToFlow_When_ExplicitlyDisabled()
    {
        // arrange - explicitly disable the circuit breaker
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddCircuitBreaker(o => o.Enabled = false);
            b.AddEventHandler<AlwaysSucceedHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new TestEvent { Data = "disabled-cb" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task CircuitBreaker_Should_Survive_When_SensitivelyConfiguredAndHandlerFails()
    {
        // arrange - configure a very sensitive circuit breaker
        var counter = new InvocationCounter();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(counter);
            b.AddCircuitBreaker(o =>
            {
                o.FailureRatio = 0.1;
                o.MinimumThroughput = 2;
                o.BreakDuration = TimeSpan.FromSeconds(5);
                o.SamplingDuration = TimeSpan.FromSeconds(10);
            });
            b.AddEventHandler<AlwaysThrowingHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send failing messages
        for (var i = 0; i < 5; i++)
        {
            await bus.PublishAsync(new TestEvent { Data = $"fail-{i}" }, CancellationToken.None);
        }

        // Wait for at least MinimumThroughput (2) handler invocations instead of
        // relying on a fixed delay which is flaky on slow CI runners.
        Assert.True(
            await counter.WaitForCountAsync(2, s_timeout),
            $"Expected at least 2 invocations (MinimumThroughput), but got {counter.Count}");
    }

    [Fact]
    public async Task CircuitBreaker_Should_AllowMessagesToFlowAgain_When_RecoveredFromOpenState()
    {
        // arrange - use a handler that fails first few times then succeeds
        var counter = new InvocationCounter();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(counter);
            b.Services.AddSingleton(recorder);
            b.AddCircuitBreaker(o =>
            {
                o.FailureRatio = 1.0;
                o.MinimumThroughput = 2;
                o.BreakDuration = TimeSpan.FromSeconds(1);
                o.SamplingDuration = TimeSpan.FromSeconds(30);
            });
            b.Services.AddSingleton<ConditionallyThrowingHandler>();
            b.AddEventHandler<ConditionallyThrowingHandler>();
        });
        var handler = provider.GetRequiredService<ConditionallyThrowingHandler>();
        handler.ThrowCount = 2;

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send failing messages to trip the circuit breaker
        await bus.PublishAsync(new TestEvent { Data = "first" }, CancellationToken.None);

        await bus.PublishAsync(new TestEvent { Data = "second" }, CancellationToken.None);

        // wait for the break duration to elapse so the circuit transitions from Open to HalfOpen
        await Task.Delay(TimeSpan.FromSeconds(2));

        // should now allow a message through (half-open -> closed on success)
        var waitForBreak = recorder.WaitAsync(s_timeout);
        await bus.PublishAsync(new TestEvent { Data = "third" }, CancellationToken.None);
        Assert.True(await waitForBreak);

        Assert.Single(recorder.Messages);
    }

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<object> Messages { get; } = [];

        public void Record(object message)
        {
            Messages.Add(message);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
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
    // Test Types
    // ============================================================

    public sealed class InvocationCounter
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

    /// <summary>
    /// Handler that throws for the first N invocations, then succeeds.
    /// </summary>
    public sealed class ConditionallyThrowingHandler(InvocationCounter counter, MessageRecorder recorder)
        : IEventHandler<TestEvent>
    {
        public int ThrowCount { get; set; } = int.MaxValue;

        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            var invocation = counter.Count;
            counter.Increment();

            if (invocation < ThrowCount)
            {
                throw new InvalidOperationException($"Failure #{invocation}");
            }

            recorder.Record(message);
            return default;
        }
    }

    public sealed class AlwaysThrowingHandler(InvocationCounter counter) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            counter.Increment();
            throw new InvalidOperationException("Always fails");
        }
    }

    public sealed class AlwaysSucceedHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class TestEvent
    {
        public required string Data { get; init; }
    }
}
