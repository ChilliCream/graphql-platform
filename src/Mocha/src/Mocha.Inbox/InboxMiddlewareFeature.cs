using Mocha.Features;

namespace Mocha.Inbox;

/// <summary>
/// A pooled feature that controls whether the inbox middleware should be bypassed for a given receive.
/// </summary>
/// <remarks>
/// Attach this feature to the receive context's feature collection and set <see cref="SkipInbox"/>
/// to <c>true</c> to process the message without checking the inbox for duplicates.
/// The feature is pooled and automatically reset between uses.
/// </remarks>
public sealed class InboxMiddlewareFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets a value indicating whether the inbox deduplication check should be skipped for the current receive.
    /// </summary>
    public bool SkipInbox { get; set; }

    /// <summary>
    /// Initializes the feature from the pool, resetting <see cref="SkipInbox"/> to <c>false</c>.
    /// </summary>
    /// <param name="state">The initialization state provided by the feature pool (unused).</param>
    public void Initialize(object state)
    {
        SkipInbox = false;
    }

    /// <summary>
    /// Resets the feature state before returning it to the pool, clearing <see cref="SkipInbox"/> to <c>false</c>.
    /// </summary>
    public void Reset()
    {
        SkipInbox = false;
    }
}
