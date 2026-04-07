using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Executes an action with retry logic based on exception policy rules.
/// Returns normally on success or discard. Throws on dead-letter, no match, or exhausted retries.
/// </summary>
internal static class RetryExecutor
{
    public static ValueTask ExecuteAsync<TState>(
        ImmutableArray<ExceptionPolicyRule> rules,
        TState state,
        Func<TState, ValueTask> action,
        Action<TState, int>? onRetry,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(rules, state, action, onRetry, TimeProvider.System, cancellationToken);
    }

    public static async ValueTask ExecuteAsync<TState>(
        ImmutableArray<ExceptionPolicyRule> rules,
        TState state,
        Func<TState, ValueTask> action,
        Action<TState, int>? onRetry,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                await action(state);
                return;
            }
            catch (Exception ex)
            {
                // Match exception against policy rules.
                var rule = ExceptionPolicyMatcher.Match(rules, ex);

                // No matching rule - no policy for this exception, let it propagate.
                if (rule is null)
                {
                    throw;
                }

                // Discard: swallow the exception (always immediate, no retry chaining exists).
                if (rule.Terminal == TerminalAction.Discard)
                {
                    return;
                }

                // No retry configured for this rule, or retry explicitly disabled.
                // Terminal (e.g. DeadLetter) is metadata for the fault middleware downstream.
                if (rule.Retry is null or { Enabled: false })
                {
                    throw;
                }

                attempts++;

                // Use the rule's retry config (fully populated by the builder).
                var retryConfig = rule.Retry;

                if (attempts > (retryConfig.Attempts ?? RetryPolicyDefaults.Attempts))
                {
                    throw;
                }

                // Calculate delay.
                var delay = CalculateDelay(attempts, retryConfig);

                // Notify caller of retry attempt.
                onRetry?.Invoke(state, attempts);

                if (delay > TimeSpan.Zero)
                {
                    await DelayAsync(timeProvider, delay, cancellationToken);
                }
            }
        }
    }

    private static async Task DelayAsync(TimeProvider timeProvider, TimeSpan delay, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        await using var timer = timeProvider.CreateTimer(
            static s => ((TaskCompletionSource)s!).TrySetResult(),
            tcs,
            delay,
            Timeout.InfiniteTimeSpan);
        await using var registration = cancellationToken.Register(
            static s => ((TaskCompletionSource)s!).TrySetCanceled(),
            tcs);
        await tcs.Task.ConfigureAwait(false);
    }

    internal static TimeSpan CalculateDelay(int attempt, RetryPolicyConfig config)
    {
        // Explicit intervals take precedence.
        if (config.Intervals is { Length: > 0 } intervals)
        {
            var index = Math.Min(attempt - 1, intervals.Length - 1);
            return intervals[index];
        }

        // Calculate based on backoff type.
        var baseDelay = config.Delay ?? RetryPolicyDefaults.Delay;
        var backoff = config.Backoff ?? RetryPolicyDefaults.Backoff;
        var maxDelay = config.MaxDelay ?? RetryPolicyDefaults.MaxDelay;
        var useJitter = config.UseJitter ?? RetryPolicyDefaults.UseJitter;

        var delay = backoff switch
        {
            RetryBackoffType.Constant => baseDelay,
            RetryBackoffType.Linear => baseDelay * attempt,
            RetryBackoffType.Exponential => baseDelay * Math.Pow(2, attempt - 1),
            _ => baseDelay * Math.Pow(2, attempt - 1)
        };

        // Cap at max delay.
        if (delay > maxDelay)
        {
            delay = maxDelay;
        }

        // Add jitter: +/- 25%.
        if (useJitter)
        {
            var jitterRange = delay.TotalMilliseconds * 0.25;
            var jitter = ((Random.Shared.NextDouble() * 2) - 1) * jitterRange;
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        return delay;
    }
}
