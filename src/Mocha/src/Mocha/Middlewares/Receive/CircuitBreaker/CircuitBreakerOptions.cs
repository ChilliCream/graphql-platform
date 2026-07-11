namespace Mocha;

/// <summary>
/// Options for configuring the circuit breaker middleware that halts message processing when the failure rate exceeds a threshold.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets whether the circuit breaker is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the failure ratio threshold (0.0 to 1.0) that triggers the circuit breaker to open.
    /// </summary>
    public double? FailureRatio { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of messages that must be processed during the sampling window before the circuit breaker evaluates the failure ratio.
    /// </summary>
    public int? MinimumThroughput { get; set; }

    /// <summary>
    /// Gets or sets the duration of the sampling window used to calculate the failure ratio.
    /// </summary>
    public TimeSpan? SamplingDuration { get; set; }

    /// <summary>
    /// Gets or sets the duration the circuit breaker remains open before transitioning to half-open.
    /// </summary>
    public TimeSpan? BreakDuration { get; set; }

    /// <summary>
    /// Provides the default values for circuit breaker options.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// The default failure ratio threshold (50%).
        /// </summary>
        public static double FailureRatio = 0.5;

        /// <summary>
        /// The default minimum throughput before evaluation.
        /// </summary>
        public static int MinimumThroughput = 10;

        /// <summary>
        /// The default sampling window duration.
        /// </summary>
        public static TimeSpan SamplingDuration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The default break duration.
        /// </summary>
        public static TimeSpan BreakDuration = TimeSpan.FromSeconds(10);
    }
}
