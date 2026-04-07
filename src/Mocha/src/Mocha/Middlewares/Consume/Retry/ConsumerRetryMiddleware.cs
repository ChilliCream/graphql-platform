using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha;

/// <summary>
/// A consumer middleware that implements in-process retry with configurable backoff strategies
/// when transient failures occur.
/// </summary>
internal sealed class ConsumerRetryMiddleware(ImmutableArray<ExceptionPolicyRule> exceptionPolicyRules)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        // Read delayed retry count from headers (set by redelivery middleware).
        var delayedRetryCount = 0;

        if (context.Headers.TryGetValue(MessageHeaders.Retry.DelayedRetryCount.Key, out var headerValue))
        {
            delayedRetryCount = headerValue switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                _ => 0
            };
        }

        // Expose retry state to handlers via features.
        var retryState = context.Features.GetOrSet<RetryFeature>();
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

                var middleware = new ConsumerRetryMiddleware(feature.Rules.ToImmutableArray());

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Retry");
}

file static class Extensions
{
    /// <summary>
    /// Resolves the bus-level exception policy feature, if configured.
    /// </summary>
    public static ExceptionPolicyFeature? GetExceptionPolicyFeature(this ConsumerMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        if (busFeatures.TryGet(out ExceptionPolicyFeature? busFeature))
        {
            return busFeature;
        }

        return null;
    }
}
