using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha;

/// <summary>
/// A consumer middleware that implements in-process retry with configurable backoff strategies
/// when transient failures occur.
/// </summary>
internal sealed class ConsumerRetryMiddleware(IReadOnlyList<ExceptionPolicyRule> exceptionPolicyRules)
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
        var retryState = context.Features.GetOrSet<RetryRuntimeFeature>();
        retryState.DelayedRetryCount = delayedRetryCount;
        retryState.ImmediateRetryCount = 0;

        await RetryExecutor.ExecuteAsync(
            exceptionPolicyRules,
            (next, context, retryState),
            static (s) => s.next(s.context),
            static (s, attempts) => s.retryState.ImmediateRetryCount = attempts,
            context.CancellationToken);
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var feature = context.GetExceptionPolicyFeature();

                if (feature is null)
                {
                    // No exception policy configured - skip retry middleware entirely.
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
