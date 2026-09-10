using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeDaemonRetryPolicy"/>'s pure backoff math and
/// transient-reason classification: the delay starts at 250 ms, doubles per
/// consecutive failure, and never exceeds the 60-second cap; only the
/// documented safe scheduling/handoff reasons are treated as retryable.
/// </summary>
public sealed class MailWakeDaemonRetryPolicyTests
{
    [Theory]
    [InlineData(0, 250)]
    [InlineData(1, 250)]
    [InlineData(2, 500)]
    [InlineData(3, 1_000)]
    [InlineData(4, 2_000)]
    [InlineData(5, 4_000)]
    [InlineData(6, 8_000)]
    [InlineData(7, 16_000)]
    [InlineData(8, 32_000)]
    public void ComputeDelay_Should_DoubleEachConsecutiveFailure_Until_ItReachesTheCap(
        int consecutiveFailures, int expectedMilliseconds)
    {
        // act
        var delay = MailWakeDaemonRetryPolicy.ComputeDelay(consecutiveFailures);

        // assert
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), delay);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(1000)]
    public void ComputeDelay_Should_NeverExceedTheSixtySecondCap(int consecutiveFailures)
    {
        // act
        var delay = MailWakeDaemonRetryPolicy.ComputeDelay(consecutiveFailures);

        // assert
        Assert.Equal(MailWakeDaemonRetryPolicy.MaxDelay, delay);
    }

    [Theory]
    [InlineData("busy", true)]
    [InlineData("capacity-dropped", true)]
    [InlineData("access-denied", true)]
    [InlineData("no-endpoint", false)]
    [InlineData("session-gone", false)]
    [InlineData("unsupported", false)]
    [InlineData("Timeout", false)]
    [InlineData("InvalidAuth", false)]
    [InlineData(null, false)]
    public void IsTransientOffer_Should_ClassifyOnlySafeSchedulingAndHandoffReasons(string? lastError, bool expected)
    {
        // act
        var actual = MailWakeDaemonRetryPolicy.IsTransientOffer(lastError);

        // assert
        Assert.Equal(expected, actual);
    }
}
