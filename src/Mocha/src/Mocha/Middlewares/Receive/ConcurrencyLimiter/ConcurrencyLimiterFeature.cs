using Mocha.Features;

namespace Mocha;

/// <summary>
/// A feature that exposes the concurrency limiter configuration for a receive endpoint.
/// </summary>
public sealed class ConcurrencyLimiterFeature : ISealable
{
    private readonly ConcurrencyLimiterOptions _options = new();

    /// <inheritdoc />
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Gets whether the concurrency limiter is enabled, or <c>null</c> if not configured.
    /// </summary>
    public bool? Enabled => _options.Enabled;

    /// <summary>
    /// Gets the maximum number of concurrent messages allowed, or <c>null</c> if not configured.
    /// </summary>
    public int? MaxConcurrency => _options.MaxConcurrency;

    /// <inheritdoc />
    public void Seal()
    {
        IsReadOnly = true;
    }

    /// <summary>
    /// Applies configuration to the concurrency limiter options.
    /// </summary>
    /// <param name="configure">An action that modifies the concurrency limiter options.</param>
    /// <exception cref="InvalidOperationException">Thrown if the feature has been sealed.</exception>
    public void Configure(Action<ConcurrencyLimiterOptions> configure)
    {
        if (IsReadOnly)
        {
            throw ThrowHelper.FeatureIsReadOnly();
        }

        configure(_options);
    }
}
