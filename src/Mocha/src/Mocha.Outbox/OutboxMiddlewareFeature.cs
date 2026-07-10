using Mocha.Features;

namespace Mocha.Outbox;

/// <summary>
/// A pooled feature that controls whether the outbox middleware should be bypassed for a given dispatch.
/// </summary>
/// <remarks>
/// Attach this feature to the dispatch context's feature collection and set <see cref="SkipOutbox"/>
/// to <c>true</c> to send the message directly without persisting it to the outbox.
/// The feature is pooled and automatically reset between uses.
/// </remarks>
public sealed class OutboxMiddlewareFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets a value indicating whether the outbox persistence step should be skipped for the current dispatch.
    /// </summary>
    public bool SkipOutbox { get; set; }

    /// <summary>
    /// Initializes the feature from the pool, resetting <see cref="SkipOutbox"/> to <c>false</c>.
    /// </summary>
    /// <param name="state">The initialization state provided by the feature pool (unused).</param>
    public void Initialize(object state)
    {
        SkipOutbox = false;
    }

    /// <summary>
    /// Resets the feature state before returning it to the pool, clearing <see cref="SkipOutbox"/> to <c>false</c>.
    /// </summary>
    public void Reset()
    {
        SkipOutbox = false;
    }
}
