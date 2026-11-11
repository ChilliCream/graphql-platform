using Mocha.Threading;

namespace Mocha.Tests.Threading;

public sealed class ExponentialBackoffTests
{
    [Theory]
    [InlineData(0, 2_000)]
    [InlineData(1, 2_000)]
    [InlineData(2, 4_000)]
    [InlineData(3, 8_000)]
    [InlineData(4, 10_000)]
    [InlineData(5, 10_000)]
    [InlineData(40, 10_000)]
    [InlineData(int.MaxValue, 10_000)]
    public void CalculateDelay_Should_StayBetweenHalfAndFullCeiling_When_AttemptFails(
        int attempt,
        int ceilingMilliseconds)
    {
        // arrange
        var initialDelay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var delays = Enumerable.Range(0, 100)
            .Select(_ => ExponentialBackoff.CalculateDelay(attempt, initialDelay, maxDelay).TotalMilliseconds)
            .ToList();

        // assert
        Assert.All(delays, delay => Assert.InRange(delay, ceilingMilliseconds / 2d, ceilingMilliseconds));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(40)]
    public void CalculateDelay_Should_Vary_When_AttemptIsRepeated(int attempt)
    {
        // arrange
        var initialDelay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var delays = Enumerable.Range(0, 100)
            .Select(_ => ExponentialBackoff.CalculateDelay(attempt, initialDelay, maxDelay))
            .Distinct()
            .ToList();

        // assert
        Assert.True(delays.Count > 1, "The delays are not randomized.");
    }

    [Fact]
    public void CalculateDelay_Should_ReturnZero_When_DelayIsZero()
    {
        // arrange
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var delay = ExponentialBackoff.CalculateDelay(int.MaxValue, TimeSpan.Zero, maxDelay);

        // assert
        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void CalculateDelay_Should_Throw_When_DelayIsNegative()
    {
        // arrange
        var delay = TimeSpan.FromSeconds(-1);
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ExponentialBackoff.CalculateDelay(1, delay, maxDelay));

        // assert
        Assert.Equal(
            "The delay must not be negative. (Parameter 'delay')\nActual value was -00:00:01.",
            exception.Message.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void CalculateDelay_Should_Throw_When_MaxDelayIsNegative()
    {
        // arrange
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(-10);

        // act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ExponentialBackoff.CalculateDelay(1, delay, maxDelay));

        // assert
        Assert.Equal(
            "The delay must not be negative. (Parameter 'maxDelay')\nActual value was -00:00:10.",
            exception.Message.ReplaceLineEndings("\n"));
    }
}
