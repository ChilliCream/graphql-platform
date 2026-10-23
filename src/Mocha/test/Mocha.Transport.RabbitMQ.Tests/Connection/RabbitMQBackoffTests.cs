namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQBackoffTests
{
    [Theory]
    [InlineData(1, 1_000)]
    [InlineData(2, 2_000)]
    [InlineData(3, 4_000)]
    [InlineData(4, 8_000)]
    [InlineData(5, 10_000)]
    [InlineData(40, 10_000)]
    public void GetDelay_Should_StayWithinJitterOfExponentialDelay_When_AttemptFails(
        int attempt,
        int expectedMilliseconds)
    {
        // arrange
        var initialDelay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(10);

        // act
        var delays = Enumerable.Range(0, 100)
            .Select(_ => RabbitMQBackoff.GetDelay(attempt, initialDelay, maxDelay).TotalMilliseconds)
            .ToList();

        // assert
        Assert.InRange(delays.Min(), expectedMilliseconds * 0.8, expectedMilliseconds * 1.2);
        Assert.InRange(delays.Max(), expectedMilliseconds * 0.8, expectedMilliseconds * 1.2);
        Assert.True(delays.Distinct().Count() > 1, "The delays are not randomized.");
    }
}
