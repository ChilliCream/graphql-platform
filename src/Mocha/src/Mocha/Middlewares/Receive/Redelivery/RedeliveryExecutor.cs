using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Provides delay calculation and exception evaluation logic for the redelivery middleware.
/// </summary>
internal static class RedeliveryExecutor
{
    /// <summary>
    /// Evaluates exception policy rules and determines whether to rethrow, discard, or redeliver.
    /// </summary>
    /// <param name="rules">The exception policy rules to evaluate.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="delayedRetryCount">The current delayed retry count.</param>
    /// <returns>A <see cref="RedeliveryDecision"/> indicating the action to take.</returns>
    internal static RedeliveryDecision Evaluate(
        ImmutableArray<ExceptionPolicyRule> rules,
        Exception exception,
        int delayedRetryCount)
    {
        // Match exception against policy rules.
        var rule = ExceptionPolicyMatcher.Match(rules, exception);

        // No matching rule — no policy for this exception.
        if (rule is null)
        {
            return RedeliveryDecision.Rethrow;
        }

        // Discard: swallow at receive level.
        if (rule.Terminal == TerminalAction.Discard)
        {
            return RedeliveryDecision.Discard;
        }

        // DeadLetter: skip redelivery, let fault middleware handle.
        if (rule.Terminal == TerminalAction.DeadLetter)
        {
            return RedeliveryDecision.Rethrow;
        }

        // No redelivery configured for this rule, or redelivery explicitly disabled.
        if (rule.Redelivery is null or { Enabled: false })
        {
            return RedeliveryDecision.Rethrow;
        }

        var redeliveryConfig = rule.Redelivery;

        // Check if redelivery attempts remain.
        var maxAttempts = redeliveryConfig.Attempts ?? redeliveryConfig.Intervals?.Length ?? 0;

        if (delayedRetryCount >= maxAttempts)
        {
            return RedeliveryDecision.Rethrow;
        }

        // Calculate the delay for this redelivery attempt.
        var delay = CalculateDelay(delayedRetryCount, redeliveryConfig);
        return RedeliveryDecision.Redeliver(delay);
    }

    /// <summary>
    /// Parses a delayed retry count from a message header value.
    /// </summary>
    /// <param name="headerValue">The raw header value.</param>
    /// <returns>The parsed integer count, or 0 if the value cannot be parsed.</returns>
    internal static int ParseDelayedRetryCount(object? headerValue)
    {
        return headerValue switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            _ => 0
        };
    }

    internal static TimeSpan CalculateDelay(int attempt, RedeliveryPolicyConfig config)
    {
        TimeSpan baseDelay;

        if (config.Intervals is { Length: > 0 } intervals)
        {
            // Explicit intervals: use array index, clamp to last.
            baseDelay = intervals[Math.Min(attempt, intervals.Length - 1)];
        }
        else
        {
            // Calculated: BaseDelay * (attempt + 1).
            var configuredBaseDelay = config.BaseDelay ?? RedeliveryPolicyDefaults.Intervals[0];
            baseDelay = configuredBaseDelay * (attempt + 1);
        }

        // Cap by MaxDelay.
        var maxDelay = config.MaxDelay ?? RedeliveryPolicyDefaults.MaxDelay;

        if (baseDelay > maxDelay)
        {
            baseDelay = maxDelay;
        }

        // Add jitter: +/- 25%.
        var useJitter = config.UseJitter ?? RedeliveryPolicyDefaults.UseJitter;

        if (useJitter)
        {
            var jitterRange = baseDelay.TotalMilliseconds * 0.25;
            var jitter = ((Random.Shared.NextDouble() * 2) - 1) * jitterRange;
            baseDelay = TimeSpan.FromMilliseconds(Math.Max(0, baseDelay.TotalMilliseconds + jitter));
        }

        return baseDelay;
    }
}
