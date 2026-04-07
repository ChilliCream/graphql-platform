using System.Collections.Immutable;
using Microsoft.Extensions.Time.Testing;

namespace Mocha.Tests.Middlewares.Consume.Retry;

public sealed class RetryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Succeed_When_ActionDoesNotThrow()
    {
        // arrange
        var rules = BuildRules(p => p.On<InvalidOperationException>().Retry());
        var counter = new Counter();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();
                return ValueTask.CompletedTask;
            },
            onRetry: null,
            cancellationToken: default);

        // assert
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_NoRuleMatchesException()
    {
        // arrange
        var rules = BuildRules(p => p.On<ArgumentException>().Retry());

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    0,
                    static (_) => throw new InvalidOperationException("no match"),
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_When_TerminalIsDiscard()
    {
        // arrange
        var rules = BuildRules(p => p.On<InvalidOperationException>().Discard());

        // act - should not throw
        await RetryExecutor.ExecuteAsync(
            rules,
            0,
            static (_) => throw new InvalidOperationException("discard me"),
            onRetry: null,
            cancellationToken: default);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_TerminalIsDeadLetter()
    {
        // arrange
        var rules = BuildRules(p => p.On<InvalidOperationException>().DeadLetter());

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    0,
                    static (_) => throw new InvalidOperationException("dead letter"),
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_RetryIsDisabled()
    {
        // arrange - Redeliver() sets Retry.Enabled = false
        var rules = BuildRules(p => p.On<InvalidOperationException>().Redeliver());

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    0,
                    static (_) => throw new InvalidOperationException("no retry"),
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryAndSucceed_When_ActionFailsThenSucceeds()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant)
        );
        var counter = new Counter();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();

                if (s.Count == 1)
                {
                    throw new InvalidOperationException("transient");
                }

                return ValueTask.CompletedTask;
            },
            onRetry: null,
            cancellationToken: default);

        // assert
        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_AllRetriesExhausted()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(2, TimeSpan.Zero, RetryBackoffType.Constant)
        );
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("always fails");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_InvokeOnRetry_When_Retrying()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant)
        );
        var counter = new Counter();
        var retryAttempts = new List<int>();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            (Counter: counter, Attempts: retryAttempts),
            static (s) =>
            {
                s.Counter.Increment();

                if (s.Counter.Count <= 2)
                {
                    throw new InvalidOperationException("transient");
                }

                return ValueTask.CompletedTask;
            },
            onRetry: static (s, attempt) => s.Attempts.Add(attempt),
            cancellationToken: default);

        // assert
        Assert.Equal(3, counter.Count);
        Assert.Equal(2, retryAttempts.Count);
        Assert.Equal(1, retryAttempts[0]);
        Assert.Equal(2, retryAttempts[1]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotInvokeOnRetry_When_ActionSucceeds()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant)
        );

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            0,
            static (_) => ValueTask.CompletedTask,
            onRetry: static (_, _) => throw new InvalidOperationException("should not be called"),
            cancellationToken: default);
    }

    // -- CalculateDelay tests --

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void CalculateDelay_Should_ReturnConstantDelay_When_BackoffIsConstant(int attempt)
    {
        // arrange
        var config = new RetryPolicyConfig
        {
            Backoff = RetryBackoffType.Constant,
            Delay = TimeSpan.FromMilliseconds(100),
            UseJitter = false,
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        // act
        var delay = RetryExecutor.CalculateDelay(attempt, config);

        // assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), delay);
    }

    [Fact]
    public void CalculateDelay_Should_ReturnLinearDelay_When_BackoffIsLinear()
    {
        // arrange
        var config = new RetryPolicyConfig
        {
            Backoff = RetryBackoffType.Linear,
            Delay = TimeSpan.FromMilliseconds(100),
            UseJitter = false,
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        // act & assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), RetryExecutor.CalculateDelay(1, config));
        Assert.Equal(TimeSpan.FromMilliseconds(200), RetryExecutor.CalculateDelay(2, config));
        Assert.Equal(TimeSpan.FromMilliseconds(300), RetryExecutor.CalculateDelay(3, config));
    }

    [Fact]
    public void CalculateDelay_Should_ReturnExponentialDelay_When_BackoffIsExponential()
    {
        // arrange
        var config = new RetryPolicyConfig
        {
            Backoff = RetryBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(100),
            UseJitter = false,
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        // act & assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), RetryExecutor.CalculateDelay(1, config));
        Assert.Equal(TimeSpan.FromMilliseconds(200), RetryExecutor.CalculateDelay(2, config));
        Assert.Equal(TimeSpan.FromMilliseconds(400), RetryExecutor.CalculateDelay(3, config));
    }

    [Fact]
    public void CalculateDelay_Should_CapAtMaxDelay_When_DelayExceedsMax()
    {
        // arrange
        var config = new RetryPolicyConfig
        {
            Backoff = RetryBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(2),
            UseJitter = false
        };

        // act
        var delay = RetryExecutor.CalculateDelay(10, config);

        // assert
        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void CalculateDelay_Should_UseExplicitIntervals_When_IntervalsProvided()
    {
        // arrange
        var config = new RetryPolicyConfig
        {
            Intervals = [TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100)],
            UseJitter = false
        };

        // act & assert
        Assert.Equal(TimeSpan.FromMilliseconds(10), RetryExecutor.CalculateDelay(1, config));
        Assert.Equal(TimeSpan.FromMilliseconds(50), RetryExecutor.CalculateDelay(2, config));
        Assert.Equal(TimeSpan.FromMilliseconds(100), RetryExecutor.CalculateDelay(3, config));
    }

    [Fact]
    public void CalculateDelay_Should_ClampToLastInterval_When_AttemptExceedsIntervalCount()
    {
        // arrange
        var config = new RetryPolicyConfig
        {
            Intervals = [TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(50)],
            UseJitter = false
        };

        // act
        var delay = RetryExecutor.CalculateDelay(3, config);

        // assert
        Assert.Equal(TimeSpan.FromMilliseconds(50), delay);
    }

    // -- Multi-rule policy scenarios --

    [Fact]
    public async Task ExecuteAsync_Should_Discard_When_SpecificExceptionMatchesDiscardRule()
    {
        // arrange
        var rules = BuildRules(p =>
        {
            p.On<ArgumentException>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant);
            p.On<ArgumentNullException>().Discard();
        });
        var counter = new Counter();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();
                throw new ArgumentNullException("param");
            },
            onRetry: null,
            cancellationToken: default);

        // assert
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DeadLetter_When_SpecificExceptionMatchesDeadLetterRule()
    {
        // arrange
        var rules = BuildRules(p =>
        {
            p.On<Exception>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant);
            p.On<InvalidOperationException>().DeadLetter();
        });
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("dead letter");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryWithBaseRule_When_DerivedExceptionHasNoSpecificRule()
    {
        // arrange
        var rules = BuildRules(p => p.On<Exception>().Retry(2, TimeSpan.Zero, RetryBackoffType.Constant));
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new ArgumentException("derived");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseMostSpecificRule_When_MultipleRulesMatch()
    {
        // arrange
        var rules = BuildRules(p =>
        {
            p.On<Exception>().Retry(5, TimeSpan.Zero, RetryBackoffType.Constant);
            p.On<InvalidOperationException>().Retry(1, TimeSpan.Zero, RetryBackoffType.Constant);
        });
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("specific");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Discard_When_PredicateMatches()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>(static ex => ex.Message.Contains("transient")).Discard()
        );

        // act - should not throw
        await RetryExecutor.ExecuteAsync(
            rules,
            0,
            static (_) => throw new InvalidOperationException("transient failure"),
            onRetry: null,
            cancellationToken: default);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_PredicateDoesNotMatch()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>(static ex => ex.Message.Contains("transient")).Discard()
        );

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    0,
                    static (_) => throw new InvalidOperationException("permanent failure"),
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryThenDiscard_When_DifferentExceptionsThrown()
    {
        // arrange
        var rules = BuildRules(p =>
        {
            p.On<InvalidOperationException>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant);
            p.On<ArgumentException>().Discard();
        });
        var counter = new Counter();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();

                if (s.Count == 1)
                {
                    throw new InvalidOperationException("retry this");
                }

                throw new ArgumentException("discard this");
            },
            onRetry: null,
            cancellationToken: default);

        // assert
        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryThenSucceed_When_ExceptionChangesOnRetry()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(3, TimeSpan.Zero, RetryBackoffType.Constant)
        );
        var counter = new Counter();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();

                if (s.Count <= 2)
                {
                    throw new InvalidOperationException("transient");
                }

                return ValueTask.CompletedTask;
            },
            onRetry: null,
            cancellationToken: default);

        // assert
        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_RulesListIsEmpty()
    {
        // arrange
        var rules = BuildRules(_ => { });

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    0,
                    static (_) => throw new InvalidOperationException("no rules"),
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryExactlyConfiguredAttempts_When_AlwaysFailing()
    {
        // arrange
        var rules = BuildRules(p => p.On<Exception>().Retry(5, TimeSpan.Zero, RetryBackoffType.Constant));
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("always fails");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(6, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_TrackCorrectAttemptNumbers_When_Retrying()
    {
        // arrange
        var rules = BuildRules(p => p.On<Exception>().Retry(4, TimeSpan.Zero, RetryBackoffType.Constant));
        var counter = new Counter();
        var retryAttempts = new List<int>();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    (Counter: counter, Attempts: retryAttempts),
                    static (s) =>
                    {
                        s.Counter.Increment();
                        throw new InvalidOperationException("always fails");
                    },
                    onRetry: static (s, attempt) => s.Attempts.Add(attempt),
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal([1, 2, 3, 4], retryAttempts);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseDefaultAttempts_When_ParameterlessRetry()
    {
        // arrange - parameterless Retry() uses RetryPolicyDefaults.Attempts (3)
        var rules = BuildRules(p => p.On<Exception>().Retry());
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("always fails");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        // RetryPolicyDefaults.Attempts is 3, so 1 original + 3 retries = 4
        Assert.Equal(4, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ThrowOperationCanceled_When_CancellationRequestedDuringDelay()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var rules = BuildRules(p => p.On<Exception>().Retry(3, TimeSpan.FromSeconds(10), RetryBackoffType.Constant));
        using var cts = new CancellationTokenSource();
        var counter = new Counter();

        // act - start the executor, it will block on the first retry delay
        var task = RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();
                throw new InvalidOperationException("fail");
            },
            onRetry: null,
            timeProvider,
            cts.Token);

        // Cancel while waiting for the delay
        cts.Cancel();

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotComplete_When_DelayHasNotElapsed()
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(1, TimeSpan.FromSeconds(5), RetryBackoffType.Constant)
        );

        // act - start the executor; it will block on the retry delay
        var task = RetryExecutor.ExecuteAsync(
            rules,
            0,
            static (_) => throw new InvalidOperationException("fail"),
            onRetry: null,
            timeProvider,
            cancellationToken: default);

        // assert - task should not complete until time advances
        Assert.False(task.IsCompleted);

        // Advance past the delay to unblock
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        // Second attempt also fails, retries exhausted - should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() => task.AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryWithExplicitIntervals_When_IntervalsConfigured()
    {
        // arrange
        var rules = BuildRules(p => p.On<Exception>().Retry([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero]));
        var counter = new Counter();

        // act
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();

                if (s.Count <= 2)
                {
                    throw new InvalidOperationException("transient");
                }

                return ValueTask.CompletedTask;
            },
            onRetry: null,
            cancellationToken: default);

        // assert
        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PropagateOriginalException_When_RetriesExhausted()
    {
        // arrange
        var rules = BuildRules(p => p.On<Exception>().Retry(1, TimeSpan.Zero, RetryBackoffType.Constant));

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    0,
                    static (_) => throw new InvalidOperationException("specific message"),
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal("specific message", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseDefaultPolicy_When_DefaultConfigured()
    {
        // arrange - Default() is equivalent to On<Exception>()
        var rules = BuildRules(p => p.Default().Retry(2, TimeSpan.Zero, RetryBackoffType.Constant));
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new ArgumentException("caught by default");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_OverrideDefaultWithSpecific_When_BothConfigured()
    {
        // arrange
        var rules = BuildRules(p =>
        {
            p.Default().Retry(5, TimeSpan.Zero, RetryBackoffType.Constant);
            p.On<InvalidOperationException>().Discard();
        });
        var counter = new Counter();

        // act - InvalidOperationException should be discarded, not retried
        await RetryExecutor.ExecuteAsync(
            rules,
            counter,
            static (s) =>
            {
                s.Increment();
                throw new InvalidOperationException("discarded despite default retry");
            },
            onRetry: null,
            cancellationToken: default);

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FallThroughToDefault_When_NoSpecificRuleMatches()
    {
        // arrange
        var rules = BuildRules(p =>
        {
            p.On<InvalidOperationException>().Discard();
            p.Default().Retry(2, TimeSpan.Zero, RetryBackoffType.Constant);
        });
        var counter = new Counter();

        // act & assert - ArgumentException falls through to Default (Exception) rule
        await Assert.ThrowsAsync<ArgumentException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new ArgumentException("falls to default");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryThenThrow_When_RetryWithThenDeadLetter()
    {
        // arrange - ThenDeadLetter() is metadata for fault middleware,
        // the executor should still retry before propagating.
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>().Retry(2, TimeSpan.Zero, RetryBackoffType.Constant).ThenDeadLetter()
        );
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("exhaust then dead letter");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        // 1 original + 2 retries = 3
        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryThenThrow_When_FullChainConfigured()
    {
        // arrange - Retry(2).ThenRedeliver().ThenDeadLetter()
        // The executor only handles retry; redelivery and terminal are for other layers.
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>()
                .Retry(2, TimeSpan.Zero, RetryBackoffType.Constant)
                .ThenRedeliver()
                .ThenDeadLetter()
        );
        var counter = new Counter();

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryExecutor
                .ExecuteAsync(
                    rules,
                    counter,
                    static (s) =>
                    {
                        s.Increment();
                        throw new InvalidOperationException("full chain");
                    },
                    onRetry: null,
                    cancellationToken: default)
                .AsTask()
        );

        // 1 original + 2 retries = 3
        Assert.Equal(3, counter.Count);
    }

    // -- Helpers --

    private static ImmutableArray<ExceptionPolicyRule> BuildRules(Action<ExceptionPolicyOptions> configure)
    {
        var feature = new ExceptionPolicyFeature();
        feature.Configure(configure);
        return [.. feature.Rules];
    }

    private sealed class Counter
    {
        public int Count { get; private set; }

        public void Increment() => Count++;
    }
}
