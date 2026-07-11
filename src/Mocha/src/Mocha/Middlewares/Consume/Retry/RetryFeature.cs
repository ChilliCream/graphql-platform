namespace Mocha;

/// <summary>
/// Provides retry state information to message handlers via
/// <c>context.Features.Get&lt;RetryState&gt;()</c>.
/// Null if retry is not configured via AddResilience.
/// </summary>
public sealed class RetryFeature
{
    /// <summary>
    /// Number of immediate retries already attempted for this delivery round.
    /// 0 on the first (original) attempt.
    /// </summary>
    public int ImmediateRetryCount { get; internal set; }

    /// <summary>
    /// Number of delayed redeliveries already attempted.
    /// Read from the delayed-retry-count header.
    /// </summary>
    public int DelayedRetryCount { get; internal set; }
}
