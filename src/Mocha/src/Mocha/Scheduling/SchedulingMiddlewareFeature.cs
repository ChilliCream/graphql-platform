using Mocha.Features;

namespace Mocha.Scheduling;

/// <summary>
/// A pooled feature that controls whether the scheduling middleware should be bypassed for a given dispatch.
/// </summary>
/// <remarks>
/// Attach this feature to the dispatch context's feature collection and set <see cref="SkipScheduler"/>
/// to <c>true</c> to send the message directly without persisting it to the scheduled message store.
/// The feature is pooled and automatically reset between uses.
/// </remarks>
public sealed class SchedulingMiddlewareFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets a value indicating whether the scheduling persistence step should be skipped
    /// for the current dispatch.
    /// </summary>
    public bool SkipScheduler { get; set; }

    /// <summary>
    /// Initializes the feature from the pool, resetting <see cref="SkipScheduler"/> to <c>false</c>.
    /// </summary>
    /// <param name="state">The initialization state provided by the feature pool (unused).</param>
    public void Initialize(object state)
    {
        SkipScheduler = false;
    }

    /// <summary>
    /// Resets the feature state before returning it to the pool, clearing <see cref="SkipScheduler"/> to <c>false</c>.
    /// </summary>
    public void Reset()
    {
        SkipScheduler = false;
    }
}
