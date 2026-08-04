using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Polly;
using Polly.CircuitBreaker;

namespace Mocha;

/// <summary>
/// A receive middleware that implements the circuit breaker pattern using Polly, temporarily halting
/// message processing when the failure rate exceeds configured thresholds.
/// </summary>
public sealed class ReceiveCircuitBreakerMiddleware(
    ResiliencePipeline resiliencePipeline,
    TimeSpan breakDuration,
    TimeProvider timeProvider)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        while (true)
        {
            try
            {
                await resiliencePipeline.ExecuteAsync(
                    static (state, _) => state.next(state.context),
                    (context, next),
                    context.CancellationToken);

                return;
            }
            catch (BrokenCircuitException ex)
            {
                // Honor Polly's retry hint when available, but clamp to sane delay bounds.
                var totalMilliseconds = (long)(ex.RetryAfter?.TotalMilliseconds ?? breakDuration.TotalMilliseconds);
                totalMilliseconds = totalMilliseconds is < 0 or > uint.MaxValue
                    ? (long)breakDuration.TotalMilliseconds
                    : totalMilliseconds;

                await Task.Delay(TimeSpan.FromMilliseconds(totalMilliseconds), timeProvider, context.CancellationToken);
            }
        }
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                // Feature values are resolved from endpoint -> transport -> bus to support overrides.
                var enabled = context.GetConfiguration(f => f.Enabled) ?? true;

                if (!enabled)
                {
                    return next;
                }

                var failureRatio =
                    context.GetConfiguration(f => f.FailureRatio) ?? CircuitBreakerOptions.Defaults.FailureRatio;

                var minimumThroughput =
                    context.GetConfiguration(f => f.MinimumThroughput)
                    ?? CircuitBreakerOptions.Defaults.MinimumThroughput;

                var sampleDuration =
                    context.GetConfiguration(f => f.SamplingDuration)
                    ?? CircuitBreakerOptions.Defaults.SamplingDuration;

                var breakDuration =
                    context.GetConfiguration(f => f.BreakDuration) ?? CircuitBreakerOptions.Defaults.BreakDuration;

                var pipeline = new ResiliencePipelineBuilder()
                    .AddCircuitBreaker(
                        new CircuitBreakerStrategyOptions
                        {
                            FailureRatio = failureRatio,
                            MinimumThroughput = minimumThroughput,
                            SamplingDuration = sampleDuration,
                            BreakDuration = breakDuration
                        })
                    .Build();

                var timeProvider = context.Services.GetRequiredService<TimeProvider>();

                var middleware = new ReceiveCircuitBreakerMiddleware(pipeline, breakDuration, timeProvider);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "CircuitBreaker");
}

file static class Extensions
{
    /// <summary>
    /// Resolves configuration with the most specific scope taking precedence.
    /// </summary>
    public static T? GetConfiguration<T>(
        this ReceiveMiddlewareFactoryContext context,
        Func<CircuitBreakerFeature, T> selector)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        return context.Endpoint.Features.GetFeatureValue(selector)
            ?? context.Transport.Features.GetFeatureValue(selector)
            ?? busFeatures.GetFeatureValue(selector);
    }

    private static T? GetFeatureValue<T>(this IFeatureCollection features, Func<CircuitBreakerFeature, T?> selector)
    {
        if (features.TryGet(out CircuitBreakerFeature? feature))
        {
            return selector(feature);
        }

        return default;
    }
}
