using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Per-exception redelivery configuration overrides.
/// </summary>
public sealed class RedeliveryPolicyConfig
{
    /// <summary>
    /// Gets whether redelivery is enabled for this exception. Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the number of redelivery attempts, or <c>null</c> to use global defaults.
    /// </summary>
    public int? Attempts { get; init; }

    /// <summary>
    /// Gets the base delay for redelivery, or <c>null</c> to use global defaults.
    /// </summary>
    public TimeSpan? BaseDelay { get; init; }

    /// <summary>
    /// Gets the maximum delay cap, or <c>null</c> to use global defaults.
    /// </summary>
    public TimeSpan? MaxDelay { get; init; }

    /// <summary>
    /// Gets whether jitter is enabled, or <c>null</c> to use global defaults.
    /// </summary>
    public bool? UseJitter { get; init; }

    /// <summary>
    /// Gets the explicit redelivery intervals, or <c>null</c> to use global defaults.
    /// </summary>
    public ImmutableArray<TimeSpan>? Intervals { get; init; }
}
