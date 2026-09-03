using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha;

/// <summary>
/// A consumer middleware that implements in-process retry with configurable backoff strategies
/// when transient failures occur. Each retry runs in its own service scope, so scoped state the
/// failed attempt left behind does not carry into the next one.
/// </summary>
internal sealed class ConsumerRetryMiddleware(ImmutableArray<ExceptionPolicyRule> exceptionPolicyRules)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        // Read delayed retry count from headers (set by redelivery middleware).
        var delayedRetryCount = 0;

        if (context.Headers.TryGetValue(MessageHeaders.Retry.DelayedRetryCount.Key, out var headerValue))
        {
            delayedRetryCount = RedeliveryExecutor.ParseDelayedRetryCount(headerValue);
        }

        // Expose retry state to handlers via features.
        var retryState = context.Features.GetOrSet<RetryFeature>();
        retryState.DelayedRetryCount = delayedRetryCount;
        retryState.ImmediateRetryCount = 0;

        await RetryExecutor.ExecuteAsync(
            exceptionPolicyRules,
            (next, context, retryState),
            static (s) => s.retryState.ImmediateRetryCount == 0
                ? s.next(s.context)
                : RetryInFreshScopeAsync(s.next, s.context),
            static (s, attempts) => s.retryState.ImmediateRetryCount = attempts,
            context.CancellationToken);
    }

    /// <summary>
    /// Runs one retry attempt in a new service scope, restoring the delivery scope afterwards.
    /// </summary>
    /// <remarks>
    /// The first attempt runs in the scope of the delivery. A retry re-invokes the same handler,
    /// which would otherwise observe whatever the failed attempt left in scoped services: a
    /// change tracker still holding the entities it failed to save, a unit of work half applied.
    /// A fresh scope gives the retry the same starting point a redelivery would have.
    /// </remarks>
    private static async ValueTask RetryInFreshScopeAsync(ConsumerDelegate next, IConsumeContext context)
    {
        var deliveryServices = context.Services;

        await using var scope = deliveryServices.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();

        context.Services = scope.ServiceProvider;
        accessor.Context = context;
        ResetScopeBoundFeatures(context);

        try
        {
            await next(context);
        }
        finally
        {
            // Features may now hold services of the scope being disposed; drop them so a later
            // consumer on the same delivery resolves from the restored delivery scope.
            ResetScopeBoundFeatures(context);
            accessor.Context = null;
            context.Services = deliveryServices;
        }
    }

    private static void ResetScopeBoundFeatures(IConsumeContext context)
    {
        foreach (var (_, feature) in context.Features)
        {
            if (feature is IScopeBoundFeature scopeBound)
            {
                scopeBound.ResetScope();
            }
        }
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
