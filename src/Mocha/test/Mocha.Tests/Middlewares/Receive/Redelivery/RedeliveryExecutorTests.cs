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
            Intervals = ImmutableArray.Create(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15)),
            UseJitter = false
        };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(5, config);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(15), delay);
    }

    [Theory]
    [InlineData(0, 10)]  // BaseDelay * (0 + 1) = 10min
    [InlineData(1, 20)]  // BaseDelay * (1 + 1) = 20min
    [InlineData(2, 30)]  // BaseDelay * (2 + 1) = 30min
    public void CalculateDelay_Should_ReturnLinearDelay_When_NoIntervalsProvided(
        int attempt,
        int expectedMinutes)
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
        var config = new RedeliveryPolicyConfig
        {
            UseJitter = false,
            MaxDelay = TimeSpan.FromHours(2)
        };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(0, config);

        // assert - 5min * (0 + 1) = 5min
        Assert.Equal(TimeSpan.FromMinutes(5), delay);
    }

    [Fact]
    public void CalculateDelay_Should_ScaleDefaultBaseDelay_When_AttemptIncreases()
    {
        // arrange
        var config = new RedeliveryPolicyConfig
        {
            UseJitter = false,
            MaxDelay = TimeSpan.FromHours(2)
        };

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
        var config = new RedeliveryPolicyConfig
        {
            BaseDelay = TimeSpan.FromMinutes(30),
            UseJitter = false
        };

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
            Intervals = ImmutableArray.Create(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromHours(2)),
            MaxDelay = TimeSpan.FromHours(1),
            UseJitter = false
        };

        // act
        var delay = RedeliveryExecutor.CalculateDelay(1, config);

        // assert
        Assert.Equal(TimeSpan.FromHours(1), delay);
    }
}
