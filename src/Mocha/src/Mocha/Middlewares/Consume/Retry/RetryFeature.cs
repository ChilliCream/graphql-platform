using Mocha.Features;

namespace Mocha;

/// <summary>
/// A feature that exposes the retry configuration for a consumer.
/// </summary>
public sealed class RetryFeature : ISealable
{
    private readonly RetryOptions _options = new();

    /// <inheritdoc />
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Gets whether retry is enabled, or <c>null</c> if not configured.
    /// </summary>
    public bool? Enabled => _options.Enabled;

    /// <summary>
    /// Gets the maximum retry attempts, or <c>null</c> if not configured.
    /// </summary>
    public int? MaxRetryAttempts => _options.MaxRetryAttempts;

    /// <summary>
    /// Gets the base delay between retries, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan? Delay => _options.Delay;

    /// <summary>
    /// Gets the maximum delay cap, or <c>null</c> if not configured.
    /// </summary>
    public TimeSpan? MaxDelay => _options.MaxDelay;

    /// <summary>
    /// Gets the backoff strategy, or <c>null</c> if not configured.
    /// </summary>
    public RetryBackoffType? BackoffType => _options.BackoffType;

    /// <summary>
    /// Gets whether jitter is enabled, or <c>null</c> if not configured.
    /// </summary>
    public bool? UseJitter => _options.UseJitter;

    /// <summary>
    /// Gets the explicit retry intervals, or <c>null</c> if not configured.
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
    /// Applies configuration to the retry options.
    /// </summary>
    /// <param name="configure">An action that modifies the retry options.</param>
    /// <exception cref="InvalidOperationException">Thrown if the feature has been sealed.</exception>
    public void Configure(Action<RetryOptions> configure)
    {
        if (IsReadOnly)
        {
            throw ThrowHelper.FeatureIsReadOnly();
        }

        configure(_options);
    }
}
