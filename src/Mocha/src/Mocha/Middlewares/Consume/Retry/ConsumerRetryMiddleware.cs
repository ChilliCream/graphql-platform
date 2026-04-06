using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// A consumer middleware that implements in-process retry with configurable backoff strategies
/// when transient failures occur.
/// </summary>
internal sealed class ConsumerRetryMiddleware(
    IReadOnlyList<ExceptionPolicyRule> exceptionPolicyRules)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        // Read delayed retry count from headers (set by redelivery middleware).
        var delayedRetryCount = 0;

        if (context.Headers.TryGetValue(MessageHeaders.Retry.DelayedRetryCount.Key, out var headerValue)
            && headerValue is int count)
        {
            delayedRetryCount = count;
        }

        // Expose retry state to handlers via features.
        var retryState = new RetryRuntimeFeature { DelayedRetryCount = delayedRetryCount, ImmediateRetryCount = 0 };
        context.Features.Set(retryState);

        var attempts = 0;

        while (true)
        {
            try
            {
                await next(context);
                return;
            }
            catch (Exception ex)
            {
                // Match exception against policy rules.
                var rule = ExceptionPolicyMatcher.Match(exceptionPolicyRules, ex);

                // No matching rule — no policy for this exception, let it propagate.
                if (rule is null)
                {
                    throw;
                }

                // Discard: swallow at consumer level so other consumers can still run.
                if (rule.Terminal == TerminalAction.Discard)
                {
                    return;
                }

                // DeadLetter: don't retry, let it propagate to fault middleware.
                if (rule.Terminal == TerminalAction.DeadLetter)
                {
                    throw;
                }

                // No retry configured for this rule, or retry explicitly disabled.
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

                // Update runtime feature.
                retryState.ImmediateRetryCount = attempts;

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, context.CancellationToken);
                }
            }
        }
    }

    private static TimeSpan CalculateDelay(int attempt, RetryPolicyConfig config)
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
            var jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        return delay;
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var feature = context.GetExceptionPolicyFeature();

                if (feature is null)
                {
                    // No exception policy configured — skip retry middleware entirely.
                    return next;
                }

                var middleware = new ConsumerRetryMiddleware(feature.Rules);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Retry");
}

file static class Extensions
{
    /// <summary>
    /// Resolves exception policy feature with the most specific scope taking precedence.
    /// Consumer-level ExceptionPolicyFeature overrides bus-level.
    /// </summary>
    public static ExceptionPolicyFeature? GetExceptionPolicyFeature(this ConsumerMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        // Consumer -> bus (most specific first).
        var config = context.Consumer.Configuration;
        if (config is not null)
        {
            var consumerFeatures = config.GetFeatures();
            if (consumerFeatures.TryGet(out ExceptionPolicyFeature? consumerFeature))
            {
                return consumerFeature;
            }
        }

        if (busFeatures.TryGet(out ExceptionPolicyFeature? busFeature))
        {
            return busFeature;
        }

        return null;
    }
}
