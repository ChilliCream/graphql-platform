namespace Mocha;

/// <summary>
/// Provides retry state information to message handlers via
/// <c>context.Features.Get&lt;RetryFeature&gt;()</c>.
/// Always present on a consumer attempt, even when no retry policy is configured via AddResilience.
/// </summary>
public sealed class RetryFeature
{
    /// <summary>
    /// Number of failed immediate attempts that preceded the current one in this delivery round,
    /// including attempts repeated by an <see cref="IConsumerExecutionStrategy"/>.
    /// 0 on the first (original) attempt. Once the round has failed for good it equals the total
    /// number of failed attempts.
    /// </summary>
    public int ImmediateRetryCount { get; internal set; }

    /// <summary>
    /// Number of delayed redeliveries already attempted.
    /// Read from the delayed-retry-count header.
    /// </summary>
    public int DelayedRetryCount { get; internal set; }
}
