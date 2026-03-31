using Mocha.Features;

namespace Mocha;

/// <summary>
/// A feature that exposes the redelivery configuration for a receive endpoint.
/// </summary>
public sealed class RedeliveryFeature : ISealable
{
    private readonly RedeliveryOptions _options = new();

    /// <inheritdoc />
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Gets whether redelivery is enabled, or <c>null</c> if not configured.
    /// </summary>
    public bool? Enabled => _options.Enabled;

    /// <summary>
    /// Gets the maximum redelivery attempts, or <c>null</c> if not configured.
    /// </summary>
    public int? MaxAttempts => _options.MaxAttempts;

    /// <summary>
    /// Gets the base delay for backoff calculation, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan? BaseDelay => _options.BaseDelay;

    /// <summary>
    /// Gets the maximum delay cap, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan? MaxDelay => _options.MaxDelay;

    /// <summary>
    /// Gets whether jitter is enabled, or <c>null</c> if not configured.
    /// </summary>
    public bool? UseJitter => _options.UseJitter;

    /// <summary>
    /// Gets the explicit redelivery intervals, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan[]? Intervals => _options.Intervals;

    /// <summary>
    /// Gets the exception rules configured for this feature.
    /// </summary>
    internal IReadOnlyList<ExceptionRule> ExceptionRules => _options.ExceptionRules;

    /// <inheritdoc />
    public void Seal()
    {
        IsReadOnly = true;
    }

    /// <summary>
    /// Applies configuration to the redelivery options.
    /// </summary>
    /// <param name="configure">An action that modifies the redelivery options.</param>
    /// <exception cref="InvalidOperationException">Thrown if the feature has been sealed.</exception>
    public void Configure(Action<RedeliveryOptions> configure)
    {
        if (IsReadOnly)
        {
            throw ThrowHelper.FeatureIsReadOnly();
        }

        configure(_options);
    }
}
