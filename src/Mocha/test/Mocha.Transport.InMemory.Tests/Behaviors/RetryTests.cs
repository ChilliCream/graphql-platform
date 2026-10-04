using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;
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
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
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
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
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
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
                p.On<InvalidOperationException>().DeadLetter();
            })
            .AddEventHandler<ThrowInvalidOperationHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-IGNORED" }, CancellationToken.None);

        // assert - only 1 invocation, exception propagates without retry
        await Task.Delay(500, TestContext.Current.CancellationToken);
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
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
                p.On<ArgumentException>(ex => ex.ParamName == "test").DeadLetter();
            })
            .AddEventHandler<ThrowMatchingArgumentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var matchingScope = matchingProvider.CreateScope();
        var matchingBus = matchingScope.ServiceProvider.GetRequiredService<IMessageBus>();

        await matchingBus.PublishAsync(new OrderCreated { OrderId = "ORD-MATCH" }, CancellationToken.None);

        // assert - matching predicate: no retry, only 1 invocation
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.Equal(1, matchingCounter.Count);

        // Test 2: non-matching predicate (ParamName == "other") - SHOULD retry
        await using var nonMatchingProvider = await new ServiceCollection()
            .AddSingleton(nonMatchingCounter)
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
                p.On<ArgumentException>(ex => ex.ParamName == "other").DeadLetter();
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
    public async Task Retry_Should_ExposeRetryState_When_HandlerAccessesFeatures()
    {
        // arrange
        var stateCapture = new RetryStateCapture();
        var builder = new ServiceCollection()
            .AddSingleton(stateCapture)
            .AddScoped<RetryStateCapturingConsumer>()
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(2, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
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
    public async Task Retry_Should_UseExplicitIntervals_When_IntervalsConfigured()
    {
        // arrange
        var counter = new RetryInvocationCounter();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>().Retry(
                [
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(20),
                    TimeSpan.FromMilliseconds(30)
                ]);
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
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
                p.On<ArgumentException>().DeadLetter();
            })
            .AddEventHandler<ThrowArgumentNullHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-INHERIT" }, CancellationToken.None);

        // assert - ArgumentNullException is a subclass of ArgumentException, so it's ignored: only 1 invocation
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Retry_Should_UseDefaults_When_ParameterlessAddResilience()
    {
        // arrange - default: 3 retries (from RetryPolicyDefaults.Attempts)
        var counter = new RetryInvocationCounter();
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

        // assert - default is 3 retries: 1 original + 3 retries = 4 total
        Assert.True(
            await counter.WaitForCountAsync(4, s_timeout),
            $"Expected 4 invocations (1 original + 3 default retries), but got {counter.Count}");
    }

    [Fact]
    public async Task Retry_Should_UseFreshScope_When_HandlerIsRetried()
    {
        // arrange
        // Each attempt runs in its own scope, so a retry never sees what the failed attempt left
        // in scoped services, like a DbContext with pending changes.
        var counter = new RetryInvocationCounter();
        var capture = new ScopeCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddSingleton(capture)
            .AddScoped<ScopeProbe>()
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
            })
            .AddEventHandler<ScopedThrowOnceHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-SCOPE" }, CancellationToken.None);

        // assert - two attempts, each with its own scoped instance
        Assert.True(await counter.WaitForCountAsync(2, s_timeout), "Handler was not retried");
        Assert.Equal(2, capture.ProbeIds.Distinct().Count());
    }

    [Fact]
    public async Task Consumer_Should_UseDifferentScopes_When_NoPolicyAndMultipleConsumersHandleSameMessage()
    {
        // arrange
        // The retry middleware creates the attempt scope and runs even without a policy, so two
        // handlers on one receive context get separate scopes and RetryFeature is always present.
        var capture = new NoPolicyScopeCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddScoped<ScopeProbe>()
            .AddMessageBus()
            .AddEventHandler<FirstNoPolicyScopedHandler>()
            .AddEventHandler<SecondNoPolicyScopedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-NO-POLICY-SCOPE" }, CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(2, capture.ProbeIds.Distinct().Count());
        Assert.Equal([true, true], capture.RetryFeatureWasPresent);
    }

    [Fact]
    public async Task ConsumerAttempt_Should_NotMutateReceiveContextOrNextAttempt_When_FirstAttemptFails()
    {
        // arrange
        // Attempts run on a clone of the receive context: metadata is copied and features the
        // attempt sets stay in the clone, so neither the receive context nor the next attempt sees
        // them. Features read through the fallback and the message instance are the receive
        // context's own and are shared.
        var capture = new AttemptMutationCapture();
        var services = new ServiceCollection().AddSingleton(capture);
        var builder = services.AddMessageBus()
            .AddResilience(p => p.On<Exception>()
                .Retry(1, TimeSpan.Zero, RetryBackoffType.Constant))
            .AddConsumer<MutatingRetryConsumer>();
        builder.ConfigureMessageBus(b => b.UseReceive(ReceiveStateCaptureMiddleware.Create(capture)));
        await using var provider = await builder
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new MutableRetryMessage("original"), CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await capture.ReceiveCaptured.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.False(capture.SecondAttemptFeatureWasPresent);
        Assert.NotNull(capture.ReceiveBefore.Envelope);
        Assert.Equal(capture.ReceiveBefore, capture.ReceiveAfter);
    }

    [Fact]
    public async Task ConsumerAttempt_Should_UseAndDisposeScopeDistinctFromReceiveScope_When_FirstAttemptRuns()
    {
        // arrange
        // The attempt scope is separate from the receive scope and is disposed when the attempt
        // ends, so scoped resources of a failed attempt do not outlive it into the retry.
        var capture = new ScopeLifecycleCapture();
        var services = new ServiceCollection()
            .AddSingleton(capture)
            .AddScoped<LifecycleProbe>();
        var builder = services.AddMessageBus()
            .AddEventHandler<LifecycleProbeHandler>();
        builder.ConfigureMessageBus(b => b.UseReceive(ReceiveScopeProbeMiddleware.Create()));
        await using var provider = await builder
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-LIFECYCLE" }, CancellationToken.None);

        // assert
        await capture.Disposed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.NotEqual(capture.ReceiveProbeId, capture.AttemptProbeId);
    }

    [Fact]
    public async Task ExecutionStrategy_Should_RepeatAttemptAndCountIt_When_AttemptFails()
    {
        // arrange
        // An IConsumerExecutionStrategy (the EF Core resilience integration is one) repeats the
        // attempt callback on its own. The retry middleware creates scope and clone per invocation
        // and counts each failed one, so a repetition gives the handler a new scoped instance and
        // an incremented ImmediateRetryCount like any other retry.
        var capture = new StrategyCapture();
        var services = new ServiceCollection()
            .AddSingleton(capture)
            .AddScoped<ScopeProbe>();
        var builder = services.AddMessageBus()
            .AddEventHandler<StrategyThrowOnceHandler>();
        builder.ConfigureMessageBus(b => b.ConfigureFeature(f => f.Set(
            new ConsumerExecutionStrategyFeature { Strategy = new RepeatOnceStrategy() })));
        await using var provider = await builder
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-STRATEGY" }, CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal([0, 1], capture.RetryCounts);
        Assert.Equal(2, capture.ProbeIds.Distinct().Count());
    }

    [Fact]
    public async Task Attempt_Should_SeeReceiveContextPooledFeature_When_CloneWasPooledBefore()
    {
        // arrange
        // Attempt clones are pooled ReceiveContexts whose feature reads fall back to the receive
        // context. Returning a context parks its reset IPooledFeature instances; renting it as a
        // clone must leave them parked, otherwise an empty instance shadows the receive context's.
        // That only shows on a reused instance, so the handler throws twice: the first attempt
        // parks a pooled feature on the clone and the next two reuse the clone.
        var capture = new PooledFeatureCapture();
        var services = new ServiceCollection().AddSingleton(capture);
        var builder = services.AddMessageBus()
            .AddResilience(p => p.On<Exception>()
                .Retry(2, TimeSpan.Zero, RetryBackoffType.Constant))
            .AddEventHandler<PooledFeatureHandler>();
        builder.ConfigureMessageBus(b => b.UseReceive(ReceivePooledFeatureMiddleware.Create()));
        await using var provider = await builder
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-POOLED" }, CancellationToken.None);

        // assert
        await capture.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(["ORD-POOLED", "ORD-POOLED", "ORD-POOLED"], capture.ObservedValues);
    }

    [Fact]
    public async Task Retry_Should_PropagateConversationId_When_HandlerPublishesOnRetry()
    {
        // arrange
        // ConsumeContextAccessor is scoped, so the attempt scope has its own instance and the retry
        // middleware has to set it, or a message published from a retry starts a new conversation.
        var counter = new RetryInvocationCounter();
        var capture = new ScopeCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(counter)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddResilience(p =>
            {
                p.On<Exception>()
                    .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant);
            })
            .AddConsumer<PublishOnRetryConsumer>()
            .AddConsumer<FollowUpSpy>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-CONV" }, CancellationToken.None);

        // assert - the follow-up published from the retry belongs to the original conversation
        await capture.FollowUpReceived.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.NotNull(capture.OriginalConversationId);
        Assert.Equal(capture.OriginalConversationId, capture.FollowUpConversationId);
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

    private sealed class ScopeProbe
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class ScopeCapture
    {
        public ConcurrentBag<Guid> ProbeIds { get; } = [];

        public string? OriginalConversationId { get; set; }

        public string? FollowUpConversationId { get; set; }

        public TaskCompletionSource FollowUpReceived { get; } = new();
    }

    private sealed class NoPolicyScopeCapture
    {
        public ConcurrentBag<Guid> ProbeIds { get; } = [];

        public TaskCompletionSource Completed { get; } = new();

        public ConcurrentBag<bool> RetryFeatureWasPresent { get; } = [];

        public void Record(Guid probeId, bool retryFeatureWasPresent)
        {
            RetryFeatureWasPresent.Add(retryFeatureWasPresent);
            ProbeIds.Add(probeId);

            if (ProbeIds.Count == 2)
            {
                Completed.TrySetResult();
            }
        }
    }

    private sealed class AttemptMutationCapture
    {
        public (string? MessageId, string? CorrelationId, MessageEnvelope? Envelope) ReceiveBefore { get; set; }

        public (string? MessageId, string? CorrelationId, MessageEnvelope? Envelope) ReceiveAfter { get; set; }

        public bool SecondAttemptFeatureWasPresent { get; set; }

        public TaskCompletionSource Completed { get; } = new();

        public TaskCompletionSource ReceiveCaptured { get; } = new();

        public int Attempts;
    }

    private sealed class ScopeLifecycleCapture
    {
        public Guid ReceiveProbeId { get; set; }

        public Guid AttemptProbeId { get; set; }

        public List<Guid> DisposedProbeIds { get; } = [];

        public TaskCompletionSource Disposed { get; } = new();

        public void RecordDisposed(Guid id)
        {
            DisposedProbeIds.Add(id);

            if (DisposedProbeIds.Count == 2)
            {
                Disposed.TrySetResult();
            }
        }
    }

    private sealed record RetryFollowUp;

    private sealed class StrategyCapture
    {
        public List<int> RetryCounts { get; } = [];

        public List<Guid> ProbeIds { get; } = [];

        public TaskCompletionSource Completed { get; } = new();
    }

    private sealed class PooledFeatureCapture
    {
        public List<string?> ObservedValues { get; } = [];

        public TaskCompletionSource Completed { get; } = new();

        public int Attempts;
    }

    /// <summary>
    /// Runs the attempt again once when it fails, the way a database execution strategy would.
    /// </summary>
    private sealed class RepeatOnceStrategy : IConsumerExecutionStrategy
    {
        public async ValueTask ExecuteAsync(IConsumeContext context, Func<CancellationToken, ValueTask> executeAttempt)
        {
            try
            {
                await executeAttempt(context.CancellationToken);
            }
            catch (InvalidOperationException)
            {
                await executeAttempt(context.CancellationToken);
            }
        }
    }

    private sealed class ReceivePooledFeature : IPooledFeature
    {
        public string? Value { get; set; }

        public void Initialize(object state) => Value = null;

        public void Reset() => Value = null;
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
            var retryState = context.Features.Get<RetryFeature>();
            capture.Record(retryState?.ImmediateRetryCount ?? -1);
            throw new InvalidOperationException("Fail to trigger retry");
        }
    }
    /// <summary>
    /// Records the scoped probe it was given on every attempt and throws on the first one.
    /// </summary>
    private sealed class ScopedThrowOnceHandler(
        ScopeProbe probe,
        ScopeCapture capture,
        RetryInvocationCounter counter)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            capture.ProbeIds.Add(probe.Id);
            var invocation = counter.Count;
            counter.Increment();

            if (invocation == 0)
            {
                throw new InvalidOperationException("Transient failure");
            }

            return default;
        }
    }

    private sealed class FirstNoPolicyScopedHandler(
        ScopeProbe probe,
        ConsumeContextAccessor accessor,
        NoPolicyScopeCapture capture)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            capture.Record(probe.Id, accessor.Context!.Features.Get<RetryFeature>() is not null);
            return default;
        }
    }

    private sealed class SecondNoPolicyScopedHandler(
        ScopeProbe probe,
        ConsumeContextAccessor accessor,
        NoPolicyScopeCapture capture)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            capture.Record(probe.Id, accessor.Context!.Features.Get<RetryFeature>() is not null);
            return default;
        }
    }

    private sealed class MutatingRetryConsumer(AttemptMutationCapture capture)
        : IConsumer<MutableRetryMessage>
    {
        public ValueTask ConsumeAsync(IConsumeContext<MutableRetryMessage> context)
        {
            var attempt = (IConsumeContext)context;

            if (Interlocked.Increment(ref capture.Attempts) == 1)
            {
                attempt.MessageId = "mutated-message";
                attempt.CorrelationId = "mutated-correlation";
                attempt.Envelope = null;
                attempt.Features.Set(new AttemptMutationFeature());
                throw new InvalidOperationException("Retry after mutation.");
            }

            capture.SecondAttemptFeatureWasPresent =
                attempt.Features.Get<AttemptMutationFeature>() is not null;
            capture.Completed.TrySetResult();
            return default;
        }
    }

    private sealed class LifecycleProbe(ScopeLifecycleCapture capture) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void Dispose() => capture.RecordDisposed(Id);
    }

    private sealed class LifecycleProbeHandler(
        LifecycleProbe probe,
        ScopeLifecycleCapture capture)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            capture.AttemptProbeId = probe.Id;
            return default;
        }
    }

    private sealed class ReceiveStateCaptureMiddleware(AttemptMutationCapture capture)
    {
        public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
        {
            capture.ReceiveBefore = (context.MessageId, context.CorrelationId, context.Envelope);
            await next(context);
            capture.ReceiveAfter = (context.MessageId, context.CorrelationId, context.Envelope);
            capture.ReceiveCaptured.TrySetResult();
        }

        public static ReceiveMiddlewareConfiguration Create(AttemptMutationCapture capture)
            => new(
                (_, next) =>
                {
                    var middleware = new ReceiveStateCaptureMiddleware(capture);
                    return context => middleware.InvokeAsync(context, next);
                },
                "ReceiveStateCapture");
    }

    private sealed class ReceiveScopeProbeMiddleware
    {
        public static ReceiveMiddlewareConfiguration Create()
            => new(
                static (_, next) => async context =>
                {
                    var probe = context.Services.GetRequiredService<LifecycleProbe>();
                    var capture = context.Services.GetRequiredService<ScopeLifecycleCapture>();
                    capture.ReceiveProbeId = probe.Id;
                    await next(context);
                },
                "ReceiveScopeProbe");
    }

    private sealed class StrategyThrowOnceHandler(
        ScopeProbe probe,
        ConsumeContextAccessor accessor,
        StrategyCapture capture)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            capture.ProbeIds.Add(probe.Id);
            capture.RetryCounts.Add(accessor.Context!.Features.Get<RetryFeature>()!.ImmediateRetryCount);

            if (capture.ProbeIds.Count == 1)
            {
                throw new InvalidOperationException("Repeat via strategy");
            }

            capture.Completed.TrySetResult();
            return default;
        }
    }

    /// <summary>
    /// Records the pooled feature value it sees, sets its own so the clone parks one on return,
    /// and throws twice so the clone is reused.
    /// </summary>
    private sealed class PooledFeatureHandler(ConsumeContextAccessor accessor, PooledFeatureCapture capture)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            var context = accessor.Context!;
            capture.ObservedValues.Add(context.Features.Get<ReceivePooledFeature>()?.Value);
            context.Features.Set(new ReceivePooledFeature());

            if (Interlocked.Increment(ref capture.Attempts) < 3)
            {
                throw new InvalidOperationException("Reuse the pooled clone");
            }

            capture.Completed.TrySetResult();
            return default;
        }
    }

    private sealed class ReceivePooledFeatureMiddleware
    {
        public static ReceiveMiddlewareConfiguration Create()
            => new(
                static (_, next) => context =>
                {
                    // Set() initializes pooled features, which clears Value, so it is assigned afterwards.
                    var feature = new ReceivePooledFeature();
                    context.Features.Set(feature);
                    feature.Value = "ORD-POOLED";
                    return next(context);
                },
                "ReceivePooledFeature");
    }

    private sealed record MutableRetryMessage(string Value);

    private sealed class AttemptMutationFeature;

    /// <summary>
    /// Throws on the first attempt and publishes a follow-up from the retry.
    /// </summary>
    private sealed class PublishOnRetryConsumer(ScopeCapture capture, RetryInvocationCounter counter)
        : IConsumer<OrderCreated>
    {
        public async ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            var invocation = counter.Count;
            counter.Increment();

            if (invocation == 0)
            {
                throw new InvalidOperationException("Transient failure");
            }

            capture.OriginalConversationId = context.ConversationId;
            var bus = context.Services.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new RetryFollowUp(), context.CancellationToken);
        }
    }

    private sealed class FollowUpSpy(ScopeCapture capture) : IConsumer<RetryFollowUp>
    {
        public ValueTask ConsumeAsync(IConsumeContext<RetryFollowUp> context)
        {
            capture.FollowUpConversationId = context.ConversationId;
            capture.FollowUpReceived.TrySetResult();
            return default;
        }
    }
}
