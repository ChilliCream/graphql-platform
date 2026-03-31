using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public sealed class RetryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Retry_Should_RetryHandler_When_HandlerThrowsTransientException()
    {
        // arrange
        var counter = new RetryInvocationCounter();
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
            })
            .AddEventHandler<ThrowOnceHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert - handler succeeds on 2nd attempt, so the message is recorded
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler did not record the message after retry");

        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task Retry_Should_PropagateToFault_When_AllRetriesExhausted()
    {
        // arrange
        var counter = new RetryInvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
            })
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);

        // assert - 1 original + 3 retries = 4 total invocations
        Assert.True(
            await counter.WaitForCountAsync(4, s_timeout),
            $"Expected 4 invocations (1 original + 3 retries), but got {counter.Count}");
    }

    [Fact]
    public async Task Retry_Should_SkipRetry_When_ExceptionIsIgnored()
    {
        // arrange
        var counter = new RetryInvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
                retry.On<InvalidOperationException>().Ignore();
            })
            .AddEventHandler<ThrowInvalidOperationHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-IGNORED" }, CancellationToken.None);

        // assert - only 1 invocation, exception propagates without retry
        await Task.Delay(500);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Retry_Should_SkipRetry_When_PredicateMatchesIgnoredException()
    {
        // arrange
        var matchingCounter = new RetryInvocationCounter();
        var nonMatchingCounter = new RetryInvocationCounter();

        // Test 1: matching predicate (ParamName == "test") - should NOT retry
        await using var matchingProvider = await new ServiceCollection()
            .AddSingleton(matchingCounter)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
                retry.On<ArgumentException>(ex => ex.ParamName == "test").Ignore();
            })
            .AddEventHandler<ThrowMatchingArgumentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var matchingScope = matchingProvider.CreateScope();
        var matchingBus = matchingScope.ServiceProvider.GetRequiredService<IMessageBus>();

        await matchingBus.PublishAsync(new OrderCreated { OrderId = "ORD-MATCH" }, CancellationToken.None);

        // assert - matching predicate: no retry, only 1 invocation
        await Task.Delay(500);
        Assert.Equal(1, matchingCounter.Count);

        // Test 2: non-matching predicate (ParamName == "other") - SHOULD retry
        await using var nonMatchingProvider = await new ServiceCollection()
            .AddSingleton(nonMatchingCounter)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
                retry.On<ArgumentException>(ex => ex.ParamName == "other").Ignore();
            })
            .AddEventHandler<ThrowMatchingArgumentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var nonMatchingScope = nonMatchingProvider.CreateScope();
        var nonMatchingBus = nonMatchingScope.ServiceProvider.GetRequiredService<IMessageBus>();

        await nonMatchingBus.PublishAsync(new OrderCreated { OrderId = "ORD-NOMATCH" }, CancellationToken.None);

        // assert - non-matching predicate: should retry, 4 total invocations
        Assert.True(
            await nonMatchingCounter.WaitForCountAsync(4, s_timeout),
            $"Expected 4 invocations for non-matching predicate, but got {nonMatchingCounter.Count}");
    }

    [Fact]
    public async Task Retry_Should_UseConsumerOverride_When_ConsumerHasDifferentConfig()
    {
        // arrange - bus-level: 2 retries, consumer-level: 5 retries
        var counter = new RetryInvocationCounter();
        var builder = new ServiceCollection()
            .AddSingleton(counter)
            .AddScoped<AlwaysThrowingHandler>()
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 2;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
            });

        builder.ConfigureMessageBus(b =>
        {
            b.AddHandler<AlwaysThrowingHandler>(consumer =>
            {
                consumer.AddRetry(retry =>
                {
                    retry.MaxRetryAttempts = 5;
                    retry.Delay = TimeSpan.FromMilliseconds(1);
                    retry.BackoffType = RetryBackoffType.Constant;
                    retry.UseJitter = false;
                });
            });
        });

        await using var provider = await builder.AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-OVERRIDE" }, CancellationToken.None);

        // assert - consumer override: 1 original + 5 retries = 6 total invocations
        Assert.True(
            await counter.WaitForCountAsync(6, s_timeout),
            $"Expected 6 invocations (1 original + 5 consumer-level retries), but got {counter.Count}");
    }

    [Fact]
    public async Task Retry_Should_ExposeRetryState_When_HandlerAccessesFeatures()
    {
        // arrange
        var stateCapture = new RetryStateCapture();
        var builder = new ServiceCollection()
            .AddSingleton(stateCapture)
            .AddScoped<RetryStateCapturingConsumer>()
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 2;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
            });

        builder.ConfigureMessageBus(b => b.AddHandler<RetryStateCapturingConsumer>());

        await using var provider = await builder.AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-STATE" }, CancellationToken.None);

        // assert - 3 invocations (1 original + 2 retries), all fail
        Assert.True(
            await stateCapture.WaitForCountAsync(3, s_timeout),
            $"Expected 3 invocations, but got {stateCapture.CapturedStates.Count}");

        var states = stateCapture.CapturedStates.OrderBy(s => s).ToList();
        Assert.Equal(0, states[0]); // first attempt
        Assert.Equal(1, states[1]); // first retry
        Assert.Equal(2, states[2]); // second retry
    }

    [Fact]
    public async Task Retry_Should_PassThrough_When_DisabledForConsumer()
    {
        // arrange
        var counter = new RetryInvocationCounter();
        var builder = new ServiceCollection()
            .AddSingleton(counter)
            .AddScoped<AlwaysThrowingHandler>()
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
            });

        builder.ConfigureMessageBus(b =>
            b.AddHandler<AlwaysThrowingHandler>(consumer =>
                consumer.AddRetry(retry => retry.Enabled = false)));

        await using var provider = await builder.AddInMemory().BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-DISABLED" }, CancellationToken.None);

        // assert - retry disabled: only 1 invocation
        await Task.Delay(500);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Retry_Should_UseExplicitIntervals_When_IntervalsConfigured()
    {
        // arrange
        var counter = new RetryInvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.Intervals =
                [
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(20),
                    TimeSpan.FromMilliseconds(30)
                ];
            })
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-INTERVALS" }, CancellationToken.None);

        // assert - Intervals.Length = 3 retries, so 4 total invocations
        Assert.True(
            await counter.WaitForCountAsync(4, s_timeout),
            $"Expected 4 invocations (1 original + 3 interval-based retries), but got {counter.Count}");
    }

    [Fact]
    public async Task Retry_Should_RespectInheritance_When_BaseExceptionIgnored()
    {
        // arrange - ignore ArgumentException, handler throws ArgumentNullException (subclass)
        var counter = new RetryInvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddRetry(retry =>
            {
                retry.MaxRetryAttempts = 3;
                retry.Delay = TimeSpan.FromMilliseconds(1);
                retry.BackoffType = RetryBackoffType.Constant;
                retry.UseJitter = false;
                retry.On<ArgumentException>().Ignore();
            })
            .AddEventHandler<ThrowArgumentNullHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-INHERIT" }, CancellationToken.None);

        // assert - ArgumentNullException is a subclass of ArgumentException, so it's ignored: only 1 invocation
        await Task.Delay(500);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Retry_Should_UseDefaults_When_ParameterlessAddRetry()
    {
        // arrange - default: MaxRetryAttempts = 3
        var counter = new RetryInvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddRetry()
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULT" }, CancellationToken.None);

        // assert - default is 3 retries: 1 original + 3 retries = 4 total
        Assert.True(
            await counter.WaitForCountAsync(4, s_timeout),
            $"Expected 4 invocations (1 original + 3 default retries), but got {counter.Count}");
    }

    // ============================================================
    // Test Helpers
    // ============================================================

    private sealed class RetryInvocationCounter
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

    private sealed class RetryStateCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public ConcurrentBag<int> CapturedStates { get; } = [];

        public void Record(int immediateRetryCount)
        {
            CapturedStates.Add(immediateRetryCount);
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
    private sealed class ThrowOnceHandler(RetryInvocationCounter counter, MessageRecorder recorder)
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
    private sealed class AlwaysThrowingHandler(RetryInvocationCounter counter) : IEventHandler<OrderCreated>
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
    private sealed class ThrowInvalidOperationHandler(RetryInvocationCounter counter) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            counter.Increment();
            throw new InvalidOperationException("Should be ignored");
        }
    }

    /// <summary>
    /// Always throws an ArgumentException with ParamName = "test".
    /// </summary>
    private sealed class ThrowMatchingArgumentHandler(RetryInvocationCounter counter) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            counter.Increment();
            throw new ArgumentException("Argument error", "test");
        }
    }

    /// <summary>
    /// Always throws an ArgumentNullException (subclass of ArgumentException).
    /// </summary>
    private sealed class ThrowArgumentNullHandler(RetryInvocationCounter counter) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            counter.Increment();
            throw new ArgumentNullException("param", "Null argument");
        }
    }

    /// <summary>
    /// Consumer that captures RetryState from the context features on each invocation,
    /// then always throws to force retries.
    /// </summary>
    private sealed class RetryStateCapturingConsumer(RetryStateCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            var retryState = context.Features.Get<RetryState>();
            capture.Record(retryState?.ImmediateRetryCount ?? -1);
            throw new InvalidOperationException("Fail to trigger retry");
        }
    }
}
