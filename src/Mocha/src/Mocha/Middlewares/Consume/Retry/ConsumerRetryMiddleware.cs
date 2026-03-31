using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Mocha;

/// <summary>
/// A consumer middleware that implements in-process retry using Polly, replaying the handler
/// with configurable backoff strategies when transient failures occur.
/// </summary>
internal sealed class ConsumerRetryMiddleware(
    ResiliencePipeline resiliencePipeline)
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
        // ImmediateRetryCount starts at -1 so the first increment in the callback yields 0.
        var retryState = new RetryState
        {
            DelayedRetryCount = delayedRetryCount,
            ImmediateRetryCount = -1
        };
        context.Features.Set(retryState);

        await resiliencePipeline.ExecuteAsync(
            static (state, _) =>
            {
                state.retryState.ImmediateRetryCount++;
                return state.next(state.context);
            },
            (context, next, retryState),
            context.CancellationToken);
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                // Feature values are resolved from consumer -> bus to support overrides.
                var enabled = context.GetConfiguration(f => f.Enabled) ?? true;

                if (!enabled)
                {
                    return next;
                }

                var intervals = context.GetConfiguration(f => f.Intervals);
                var maxRetryAttempts = intervals?.Length
                    ?? context.GetConfiguration(f => f.MaxRetryAttempts)
                    ?? RetryOptions.Defaults.MaxRetryAttempts;

                var delay = context.GetConfiguration(f => f.Delay)
                    ?? RetryOptions.Defaults.Delay;

                var maxDelay = context.GetConfiguration(f => f.MaxDelay)
                    ?? RetryOptions.Defaults.MaxDelay;

                var backoffType = context.GetConfiguration(f => f.BackoffType)
                    ?? RetryOptions.Defaults.BackoffType;

                var useJitter = context.GetConfiguration(f => f.UseJitter)
                    ?? RetryOptions.Defaults.UseJitter;

                // Resolve exception rules (atomically from first scope that has them).
                var exceptionRules = ResolveExceptionRules(context);

                // Map RetryBackoffType to Polly's DelayBackoffType.
                var pollyBackoffType = backoffType switch
                {
                    RetryBackoffType.Constant => DelayBackoffType.Constant,
                    RetryBackoffType.Linear => DelayBackoffType.Linear,
                    RetryBackoffType.Exponential => DelayBackoffType.Exponential,
                    _ => DelayBackoffType.Exponential
                };

                var strategyOptions = new RetryStrategyOptions
                {
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = delay,
                    MaxDelay = maxDelay,
                    BackoffType = pollyBackoffType,
                    UseJitter = useJitter
                };

                // Map Intervals to custom DelayGenerator.
                if (intervals is { Length: > 0 })
                {
                    strategyOptions.DelayGenerator = args =>
                    {
                        var index = Math.Min(args.AttemptNumber, intervals.Length - 1);
                        return new ValueTask<TimeSpan?>(intervals[index]);
                    };
                }

                // Map On<T>().Ignore() rules to ShouldHandle predicate.
                if (exceptionRules.Count > 0)
                {
                    strategyOptions.ShouldHandle = args =>
                    {
                        if (args.Outcome.Exception is { } ex
                            && ExceptionRuleMatcher.ShouldIgnore(exceptionRules, ex))
                        {
                            return new ValueTask<bool>(false);
                        }

                        return new ValueTask<bool>(args.Outcome.Exception is not null);
                    };
                }

                var pipeline = new ResiliencePipelineBuilder()
                    .AddRetry(strategyOptions)
                    .Build();

                var middleware = new ConsumerRetryMiddleware(pipeline);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Retry");

    private static IReadOnlyList<ExceptionRule> ResolveExceptionRules(ConsumerMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        // Consumer rules take precedence if present.
        if (context.Consumer.Configuration?.Features is { } consumerFeatures
            && consumerFeatures.TryGet(out RetryFeature? consumerFeature)
            && consumerFeature.ExceptionRules.Count > 0)
        {
            return consumerFeature.ExceptionRules;
        }

        if (busFeatures.TryGet(out RetryFeature? busFeature)
            && busFeature.ExceptionRules.Count > 0)
        {
            return busFeature.ExceptionRules;
        }

        return [];
    }
}

file static class Extensions
{
    /// <summary>
    /// Resolves configuration with the most specific scope taking precedence.
    /// </summary>
    public static T? GetConfiguration<T>(
        this ConsumerMiddlewareFactoryContext context,
        Func<RetryFeature, T> selector)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        // consumer -> bus (most specific first)
        if (context.Consumer.Configuration?.Features is { } consumerFeatures)
        {
            var value = consumerFeatures.GetFeatureValue(selector);

            if (value is not null)
            {
                return value;
            }
        }

        return busFeatures.GetFeatureValue(selector);
    }

    private static T? GetFeatureValue<T>(this IFeatureCollection features, Func<RetryFeature, T?> selector)
    {
        if (features.TryGet(out RetryFeature? feature))
        {
            return selector(feature);
        }

        return default;
    }
}
