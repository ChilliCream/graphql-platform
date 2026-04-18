using System.Collections.Immutable;

namespace Mocha.Tests.Middlewares.Receive.Redelivery;

public sealed class RedeliveryExecutorTests
{
    [Fact]
    public void CalculateDelay_Should_ReturnCorrectInterval_When_ExplicitIntervalsProvided()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            Intervals = ImmutableArray.Create(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(30)),
            UseJitter = false
        };

        // act & assert
        Assert.Equal(TimeSpan.FromMinutes(5), RedeliveryExecutor.CalculateDelay(0, config));
        Assert.Equal(TimeSpan.FromMinutes(15), RedeliveryExecutor.CalculateDelay(1, config));
        Assert.Equal(TimeSpan.FromMinutes(30), RedeliveryExecutor.CalculateDelay(2, config));
    }

    [Fact]
    public void CalculateDelay_Should_ClampToLastInterval_When_AttemptExceedsIntervalCount()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            Intervals = ImmutableArray.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)),
            UseJitter = false
        };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(5, config);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(15), delay);
    }

    [Theory]
    [InlineData(0, 10)] // BaseDelay * (0 + 1) = 10min
    [InlineData(1, 20)] // BaseDelay * (1 + 1) = 20min
    [InlineData(2, 30)] // BaseDelay * (2 + 1) = 30min
    public void CalculateDelay_Should_ReturnLinearDelay_When_NoIntervalsProvided(int attempt, int expectedMinutes)
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            BaseDelay = TimeSpan.FromMinutes(10),
            UseJitter = false,
            MaxDelay = TimeSpan.FromHours(2)
        };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(attempt, config);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), delay);
    }

    [Fact]
    public void CalculateDelay_Should_UseDefaultBaseDelay_When_NoBaseDelayAndNoIntervals()
    {
        // arrange - no BaseDelay, no Intervals => uses RedeliveryPolicyDefaults.Intervals[0] (5 min)
        var config = new RedeliveryPolicyConfig { UseJitter = false, MaxDelay = TimeSpan.FromHours(2) };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(0, config);

        // assert - 5min * (0 + 1) = 5min
        Assert.Equal(TimeSpan.FromMinutes(5), delay);
    }

    [Fact]
    public void CalculateDelay_Should_ScaleDefaultBaseDelay_When_AttemptIncreases()
    {
        // arrange
        var config = new RedeliveryPolicyConfig { UseJitter = false, MaxDelay = TimeSpan.FromHours(2) };

        // act & assert - 5min * (attempt + 1)
        Assert.Equal(TimeSpan.FromMinutes(5), RedeliveryExecutor.CalculateDelay(0, config));
        Assert.Equal(TimeSpan.FromMinutes(10), RedeliveryExecutor.CalculateDelay(1, config));
        Assert.Equal(TimeSpan.FromMinutes(15), RedeliveryExecutor.CalculateDelay(2, config));
    }

    [Fact]
    public void CalculateDelay_Should_CapAtMaxDelay_When_ComputedDelayExceedsMax()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            BaseDelay = TimeSpan.FromMinutes(30),
            MaxDelay = TimeSpan.FromMinutes(45),
            UseJitter = false
        };

        // act - 30min * (1 + 1) = 60min, capped at 45min
        var delay = RedeliveryExecutor.CalculateDelay(1, config);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(45), delay);
    }

    [Fact]
    public void CalculateDelay_Should_UseDefaultMaxDelay_When_NoMaxDelayConfigured()
    {
        // arrange - no MaxDelay => uses RedeliveryPolicyDefaults.MaxDelay (1 hour)
        var config = new RedeliveryPolicyConfig { BaseDelay = TimeSpan.FromMinutes(30), UseJitter = false };

        // act - 30min * (5 + 1) = 180min, capped at 60min (default)
        var delay = RedeliveryExecutor.CalculateDelay(5, config);

        // assert
        Assert.Equal(TimeSpan.FromHours(1), delay);
    }

    [Fact]
    public void CalculateDelay_Should_ReturnExactDelay_When_JitterDisabled()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            BaseDelay = TimeSpan.FromMinutes(10),
            UseJitter = false,
            MaxDelay = TimeSpan.FromHours(2)
        };

        // act - run multiple times to confirm determinism
        var delay1 = RedeliveryExecutor.CalculateDelay(0, config);
        var delay2 = RedeliveryExecutor.CalculateDelay(0, config);
        var delay3 = RedeliveryExecutor.CalculateDelay(0, config);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(10), delay1);
        Assert.Equal(delay1, delay2);
        Assert.Equal(delay2, delay3);
    }

    [Fact]
    public void CalculateDelay_Should_ReturnDelayWithinJitterBounds_When_JitterEnabled()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            BaseDelay = TimeSpan.FromMinutes(10),
            UseJitter = true,
            MaxDelay = TimeSpan.FromHours(2)
        };

        var baseExpected = TimeSpan.FromMinutes(10); // 10min * (0 + 1)
        var lowerBound = baseExpected.TotalMilliseconds * 0.75;
        var upperBound = baseExpected.TotalMilliseconds * 1.25;

        // act - run multiple times to increase confidence
        for (var i = 0; i < 100; i++)
        {
            var delay = RedeliveryExecutor.CalculateDelay(0, config);

            // assert
            Assert.InRange(delay.TotalMilliseconds, lowerBound, upperBound);
        }
    }

    [Fact]
    public void CalculateDelay_Should_ApplyJitterByDefault_When_UseJitterIsNull()
    {
        // arrange - UseJitter not set => defaults to RedeliveryPolicyDefaults.UseJitter (true)
        var config = new RedeliveryPolicyConfig
        {
            BaseDelay = TimeSpan.FromMinutes(10),
            MaxDelay = TimeSpan.FromHours(2)
        };

        var baseExpected = TimeSpan.FromMinutes(10);
        var lowerBound = baseExpected.TotalMilliseconds * 0.75;
        var upperBound = baseExpected.TotalMilliseconds * 1.25;
        var hasVariation = false;
        TimeSpan? firstDelay = null;

        // act - run multiple times; at least one should differ (proving jitter is active)
        for (var i = 0; i < 100; i++)
        {
            var delay = RedeliveryExecutor.CalculateDelay(0, config);

            Assert.InRange(delay.TotalMilliseconds, lowerBound, upperBound);

            firstDelay ??= delay;

            if (delay != firstDelay)
            {
                hasVariation = true;
            }
        }

        // assert - with jitter, we expect variation across 100 runs
        Assert.True(hasVariation, "Expected jitter to produce varying delays, but all 100 values were identical.");
    }

    [Fact]
    public void CalculateDelay_Should_CapExplicitIntervalAtMaxDelay_When_IntervalExceedsMax()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            Intervals = ImmutableArray.Create(TimeSpan.FromMinutes(5), TimeSpan.FromHours(2)),
            MaxDelay = TimeSpan.FromHours(1),
            UseJitter = false
        };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(1, config);

        // assert
        Assert.Equal(TimeSpan.FromHours(1), delay);
    }

    [Fact]
    public void ParseDelayedRetryCount_Should_ReturnIntValue_When_HeaderValueIsInt()
    {
        // act
        var result = RedeliveryExecutor.ParseDelayedRetryCount(42);

        // assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ParseDelayedRetryCount_Should_ReturnConvertedValue_When_HeaderValueIsLong()
    {
        // act
        var result = RedeliveryExecutor.ParseDelayedRetryCount(7L);

        // assert
        Assert.Equal(7, result);
    }

    [Fact]
    public void ParseDelayedRetryCount_Should_ReturnConvertedValue_When_HeaderValueIsDouble()
    {
        // act
        var result = RedeliveryExecutor.ParseDelayedRetryCount(3.0);

        // assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void ParseDelayedRetryCount_Should_ReturnZero_When_HeaderValueIsString()
    {
        // act
        var result = RedeliveryExecutor.ParseDelayedRetryCount("not a number");

        // assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseDelayedRetryCount_Should_ReturnZero_When_HeaderValueIsNull()
    {
        // act
        var result = RedeliveryExecutor.ParseDelayedRetryCount(null);

        // assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_NoRuleMatchesException()
    {
        // arrange - rules for ArgumentException, but we throw InvalidOperationException
        var rules = BuildRules(p => p.On<ArgumentException>().Redeliver());

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("no match"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnDiscard_When_TerminalIsDiscard()
    {
        // arrange
        var rules = BuildRules(p => p.On<InvalidOperationException>().Discard());

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("discard me"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Discard, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_TerminalIsDeadLetter()
    {
        // arrange
        var rules = BuildRules(p => p.On<InvalidOperationException>().DeadLetter());

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("dead letter"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_RedeliveryIsDisabled()
    {
        // arrange - Retry() sets Redelivery.Enabled = false
        var rules = BuildRules(p => p.On<InvalidOperationException>().Retry());

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("no redelivery"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_RedeliveryIsNull()
    {
        // arrange - manually build a rule with no redelivery config
        var rules = ImmutableArray.Create(
            new ExceptionPolicyRule
            {
                ExceptionType = typeof(Exception),
                Predicate = null,
                Redelivery = null
            });

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("no config"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRedeliver_When_RedeliveryConfigured()
    {
        // arrange - Redeliver() uses defaults (3 intervals, jitter enabled)
        var rules = BuildRules(p => p.On<InvalidOperationException>().Redeliver());

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("redeliver me"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Redeliver, decision.Action);
        Assert.True(decision.Delay > TimeSpan.Zero);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_AllRedeliveryAttemptsExhausted()
    {
        // arrange - 2 attempts configured, delayedRetryCount already at 2
        var rules = BuildRules(p => p.On<InvalidOperationException>().Redeliver(2, TimeSpan.FromMinutes(5)));

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("exhausted"), 2);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRedeliver_When_AttemptsRemain()
    {
        // arrange - 3 attempts configured, delayedRetryCount at 1
        var rules = BuildRules(p => p.On<InvalidOperationException>().Redeliver(3, TimeSpan.FromMinutes(5)));

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("retry"), 1);

        // assert
        Assert.Equal(RedeliveryAction.Redeliver, decision.Action);
        Assert.True(decision.Delay > TimeSpan.Zero);
    }

    [Fact]
    public void Evaluate_Should_ReturnDiscard_When_MostSpecificRuleIsDiscard()
    {
        // arrange - base rule redelivers, but more specific rule discards
        var rules = BuildRules(p =>
        {
            p.On<Exception>().Redeliver();
            p.On<InvalidOperationException>().Discard();
        });

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("specific discard"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Discard, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_RulesListIsEmpty()
    {
        // arrange
        var rules = BuildRules(_ => { });

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("no rules"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_PredicateDoesNotMatch()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>(static ex => ex.Message.Contains("transient")).Discard()
        );

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("permanent failure"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnDiscard_When_PredicateMatches()
    {
        // arrange
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>(static ex => ex.Message.Contains("transient")).Discard()
        );

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("transient failure"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Discard, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_UseIntervalLengthAsMaxAttempts_When_AttemptsNotSet()
    {
        // arrange - 3 explicit intervals, no Attempts property
        var rules = BuildRules(p =>
            p.On<InvalidOperationException>()
                .Redeliver([TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30)])
        );

        // act - attempt 2 (0-based), which is the 3rd interval → should still redeliver
        var decision2 = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("retry"), 2);

        // act - attempt 3 (0-based), all 3 intervals exhausted → should rethrow
        var decision3 = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("retry"), 3);

        // assert
        Assert.Equal(RedeliveryAction.Redeliver, decision2.Action);
        Assert.Equal(RedeliveryAction.Rethrow, decision3.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRethrow_When_MaxAttemptsIsZero()
    {
        // arrange - rule with Redelivery having no Attempts and no Intervals → maxAttempts = 0
        var rules = ImmutableArray.Create(
            new ExceptionPolicyRule
            {
                ExceptionType = typeof(Exception),
                Predicate = null,
                Redelivery = new RedeliveryPolicyConfig { Enabled = true, UseJitter = false }
            });

        // act
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("no attempts"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Rethrow, decision.Action);
    }

    [Fact]
    public void Evaluate_Should_ReturnRedeliver_When_DefaultRuleFallsThrough()
    {
        // arrange - Default() matches all exceptions, including derived types
        var rules = BuildRules(p => p.Default().Redeliver());

        // act - throw a derived exception
        var decision = RedeliveryExecutor.Evaluate(rules, new InvalidOperationException("derived"), 0);

        // assert
        Assert.Equal(RedeliveryAction.Redeliver, decision.Action);
        Assert.True(decision.Delay > TimeSpan.Zero);
    }

    private static ImmutableArray<ExceptionPolicyRule> BuildRules(Action<ExceptionPolicyOptions> configure)
    {
        var feature = new ExceptionPolicyFeature();
        feature.Configure(configure);
        return [.. feature.Rules];
    }
}
