namespace Mocha;

/// <summary>
/// Provides read-only access to transport circuit breaker configuration options.
/// </summary>
public interface IReadOnlyTransportCircuitBreakerOptions
{
    /// <summary>
    /// Gets the failure ratio threshold (0.0 to 1.0) that triggers the circuit breaker to open.
    /// </summary>
    double FailureRatio { get; }

    /// <summary>
    /// Gets the minimum number of operations required during the sampling window before evaluation.
    /// </summary>
    int MinimumThroughput { get; }

    /// <summary>
    /// Gets the duration of the sampling window used to calculate the failure ratio.
    /// </summary>
    TimeSpan SamplingDuration { get; }

    /// <summary>
    /// Gets the duration the circuit breaker remains open before transitioning to half-open.
    /// </summary>
    TimeSpan BreakDuration { get; }
}
