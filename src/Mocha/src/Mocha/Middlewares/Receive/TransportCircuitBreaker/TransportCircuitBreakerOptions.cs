namespace Mocha;

/// <summary>
/// Options for configuring the transport-level circuit breaker that monitors transport connectivity and halts message consumption when failures exceed a threshold.
/// </summary>
public class TransportCircuitBreakerOptions : IReadOnlyTransportCircuitBreakerOptions
{
    /// <inheritdoc />
    public double FailureRatio { get; set; } = 0.1;

    /// <inheritdoc />
    public int MinimumThroughput { get; set; } = 10;

    /// <inheritdoc />
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);
}
