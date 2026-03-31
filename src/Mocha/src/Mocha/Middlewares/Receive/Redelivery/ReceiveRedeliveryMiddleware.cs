using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// A receive middleware that reschedules failed messages for later delivery, releasing the
/// concurrency slot while the message waits for its next attempt.
/// </summary>
/// <remarks>
/// This middleware implements Tier 2 (delayed redelivery) of the retry model. On failure it
/// increments the <c>delayed-retry-count</c> header and dispatches the original envelope back
/// to the same endpoint with a scheduled delivery time. Request/reply messages are excluded
/// because the caller would time out waiting for a response.
/// </remarks>
internal sealed class ReceiveRedeliveryMiddleware(
    int maxAttempts,
    TimeSpan[]? intervals,
    TimeSpan resolvedBaseDelay,
    TimeSpan resolvedMaxDelay,
    bool resolvedUseJitter,
    IReadOnlyList<ExceptionRule> exceptionRules,
    TimeProvider timeProvider,
    IMessagingPools pools)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        // Read the current delayed retry count from headers.
        var delayedRetryCount = 0;

        if (context.Headers.TryGetValue(MessageHeaders.Retry.DelayedRetryCount.Key, out var headerValue))
        {
            delayedRetryCount = headerValue switch
            {
                int i => i,
                long l => (int)l,
                _ => 0
            };
        }

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Request/reply messages must not be redelivered -- the caller is waiting.
            if (context.Envelope?.ResponseAddress is not null)
            {
                throw;
            }

            // Check exception rules: if the exception is explicitly ignored, rethrow.
            if (exceptionRules.Count > 0 && ExceptionRuleMatcher.ShouldIgnore(exceptionRules, ex))
            {
                throw;
            }

            // Check if redelivery attempts remain.
            if (delayedRetryCount >= maxAttempts)
            {
                throw;
            }

            // Calculate the delay for this redelivery attempt.
            var delay = CalculateDelay(delayedRetryCount);
            var scheduledTime = timeProvider.GetUtcNow().Add(delay);

            // Update the header on the envelope so the next delivery round sees the incremented count.
            var envelope = context.Envelope;

            if (envelope is null)
            {
                throw;
            }

            envelope.Headers?.Set(MessageHeaders.Retry.DelayedRetryCount.Key, delayedRetryCount + 1);

            // Dispatch the envelope back to the same endpoint with the scheduled time.
            // Use the Source address (queue/topic) rather than the endpoint address, because
            // transports resolve dispatch endpoints from the topology resource address.
            var dispatchEndpoint = context.Runtime.GetDispatchEndpoint(context.Endpoint.Source.Address);
            var dispatchContext = pools.DispatchContext.Get();

            try
            {
                dispatchContext.Initialize(
                    context.Services,
                    dispatchEndpoint,
                    context.Runtime,
                    context.MessageType,
                    context.CancellationToken);

                dispatchContext.Envelope = envelope;
                dispatchContext.ScheduledTime = scheduledTime;

                await dispatchEndpoint.ExecuteAsync(dispatchContext);
            }
            finally
            {
                pools.DispatchContext.Return(dispatchContext);
            }

            // Mark the message as consumed so Fault/DeadLetter don't also handle it.
            context.Features.GetOrSet<ReceiveConsumerFeature>().MessageConsumed = true;
        }
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        TimeSpan baseDelay;

        if (intervals is { Length: > 0 })
        {
            // Explicit intervals: use array index, clamp to last.
            baseDelay = intervals[Math.Min(attempt, intervals.Length - 1)];
        }
        else
        {
            // Calculated: BaseDelay * (attempt + 1).
            baseDelay = resolvedBaseDelay * (attempt + 1);
        }

        // Cap by MaxDelay.
        if (baseDelay > resolvedMaxDelay)
        {
            baseDelay = resolvedMaxDelay;
        }

        // Add jitter: +/- 25%.
        if (resolvedUseJitter)
        {
            var jitterRange = baseDelay.TotalMilliseconds * 0.25;
            var jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
            baseDelay = TimeSpan.FromMilliseconds(Math.Max(0, baseDelay.TotalMilliseconds + jitter));
        }

        return baseDelay;
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

                var intervals = context.GetConfiguration(f => f.Intervals)
                    ?? RedeliveryOptions.Defaults.Intervals;

                var maxAttempts = intervals is { Length: > 0 }
                    ? intervals.Length
                    : context.GetConfiguration(f => f.MaxAttempts) ?? 0;

                if (maxAttempts <= 0 && intervals is not { Length: > 0 })
                {
                    return next;
                }

                var baseDelay = context.GetConfiguration(f => f.BaseDelay)
                    ?? TimeSpan.FromMinutes(5);

                var maxDelay = context.GetConfiguration(f => f.MaxDelay)
                    ?? RedeliveryOptions.Defaults.MaxDelay;

                var useJitter = context.GetConfiguration(f => f.UseJitter)
                    ?? RedeliveryOptions.Defaults.UseJitter;

                // Resolve exception rules (atomically from first scope that has them).
                var exceptionRules = ResolveExceptionRules(context);

                var timeProvider = context.Services.GetRequiredService<TimeProvider>();
                var pools = context.Services.GetRequiredService<IMessagingPools>();

                var middleware = new ReceiveRedeliveryMiddleware(
                    maxAttempts,
                    intervals,
                    baseDelay,
                    maxDelay,
                    useJitter,
                    exceptionRules,
                    timeProvider,
                    pools);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Redelivery");

    private static IReadOnlyList<ExceptionRule> ResolveExceptionRules(ReceiveMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        // Endpoint rules take precedence if present.
        if (context.Endpoint.Features.TryGet(out RedeliveryFeature? endpointFeature)
            && endpointFeature.ExceptionRules.Count > 0)
        {
            return endpointFeature.ExceptionRules;
        }

        if (context.Transport.Features.TryGet(out RedeliveryFeature? transportFeature)
            && transportFeature.ExceptionRules.Count > 0)
        {
            return transportFeature.ExceptionRules;
        }

        if (busFeatures.TryGet(out RedeliveryFeature? busFeature)
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
        this ReceiveMiddlewareFactoryContext context,
        Func<RedeliveryFeature, T> selector)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        return context.Endpoint.Features.GetFeatureValue(selector)
            ?? context.Transport.Features.GetFeatureValue(selector)
            ?? busFeatures.GetFeatureValue(selector);
    }

    private static T? GetFeatureValue<T>(this IFeatureCollection features, Func<RedeliveryFeature, T?> selector)
    {
        if (features.TryGet(out RedeliveryFeature? feature))
        {
            return selector(feature);
        }

        return default;
    }
}
