using Mocha.Threading;

namespace Mocha.Tests.Threading;

public sealed class ExponentialBackoffTests
{
    [Theory]
    [InlineData(1, 1_000, 2_000)]
    [InlineData(2, 2_000, 4_000)]
    [InlineData(3, 4_000, 8_000)]
    [InlineData(4, 8_000, 10_000)]
    [InlineData(5, 10_000, 10_000)]
    [InlineData(40, 10_000, 10_000)]
    [InlineData(int.MaxValue, 10_000, 10_000)]
    public void CalculateDelay_Should_StayWithinBounds_When_AttemptFails(
        int attempt,
        int minimumMilliseconds,
        int maximumMilliseconds)
    {
        // arrange
        var initialDelay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var delays = Enumerable.Range(0, 100)
            .Select(_ => ExponentialBackoff.CalculateDelay(attempt, initialDelay, maxDelay).TotalMilliseconds)
            .ToList();

        // assert
        Assert.All(delays, delay => Assert.InRange(delay, minimumMilliseconds, maximumMilliseconds));
    }

    [Fact]
    public void CalculateDelay_Should_Vary_When_AttemptIsRepeated()
    {
        // arrange
        var initialDelay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var delays = Enumerable.Range(0, 100)
            .Select(_ => ExponentialBackoff.CalculateDelay(1, initialDelay, maxDelay))
            .Distinct()
            .ToList();

        // assert
        Assert.True(delays.Count > 1, "The delays are not randomized.");
    }
}
