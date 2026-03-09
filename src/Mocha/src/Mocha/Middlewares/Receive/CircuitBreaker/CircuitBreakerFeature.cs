using Mocha.Features;

namespace Mocha;

/// <summary>
/// A feature that exposes the circuit breaker configuration for a receive endpoint.
/// </summary>
public sealed class CircuitBreakerFeature : ISealable
{
    private readonly CircuitBreakerOptions _options = new();

    /// <inheritdoc />
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Gets whether the circuit breaker is enabled, or <c>null</c> if not configured.
    /// </summary>
    public bool? Enabled => _options.Enabled;

    /// <summary>
    /// Gets the failure ratio threshold, or <c>null</c> if not configured.
    /// </summary>
    public double? FailureRatio => _options.FailureRatio;

    /// <summary>
    /// Gets the minimum throughput before the circuit breaker can activate, or <c>null</c> if not configured.
    /// </summary>
    public int? MinimumThroughput => _options.MinimumThroughput;

    /// <summary>
    /// Gets the sampling duration over which failures are measured, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan? SamplingDuration => _options.SamplingDuration;

    /// <summary>
    /// Gets the duration the circuit remains open before attempting recovery, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan? BreakDuration => _options.BreakDuration;

    /// <inheritdoc />
    public void Seal()
    {
        IsReadOnly = true;
    }

    /// <summary>
    /// Applies configuration to the circuit breaker options.
    /// </summary>
    /// <param name="configure">An action that modifies the circuit breaker options.</param>
    /// <exception cref="InvalidOperationException">Thrown if the feature has been sealed.</exception>
    public void Configure(Action<CircuitBreakerOptions> configure)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("The feature is read-only.");
        }

        configure(_options);
    }
}
