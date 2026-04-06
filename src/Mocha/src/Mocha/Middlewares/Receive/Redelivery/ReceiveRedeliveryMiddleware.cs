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
    IReadOnlyList<ExceptionPolicyRule> exceptionPolicyRules,
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

            // Match exception against policy rules.
            var rule = ExceptionPolicyMatcher.Match(exceptionPolicyRules, ex);

            // No matching rule — no policy for this exception, let it propagate.
            if (rule is null)
            {
                throw;
            }

            // Discard: swallow at receive level.
            if (rule.Terminal == TerminalAction.Discard)
            {
                var consumerFeature = context.Features.Get<ReceiveConsumerFeature>();

                if (consumerFeature is not null)
                {
                    consumerFeature.MessageConsumed = true;
                }

                return;
            }

            // DeadLetter: skip redelivery, let fault middleware handle.
            if (rule.Terminal == TerminalAction.DeadLetter)
            {
                throw;
            }

            // No redelivery configured for this rule, or redelivery explicitly disabled.
            if (rule.Redelivery is null or { Enabled: false })
            {
                throw;
            }

            var redeliveryConfig = rule.Redelivery;

            // Check if redelivery attempts remain.
            var maxAttempts = redeliveryConfig.Attempts
                ?? redeliveryConfig.Intervals?.Length
                ?? 0;

            if (delayedRetryCount >= maxAttempts)
            {
                throw;
            }

            // Calculate the delay for this redelivery attempt.
            var delay = CalculateDelay(delayedRetryCount, redeliveryConfig);
            var scheduledTime = timeProvider.GetUtcNow().Add(delay);

            // Update the header on the envelope so the next delivery round sees the incremented count.
            var envelope = context.Envelope;

            if (envelope is null)
            {
                throw;
            }

            if (envelope.Headers is null)
            {
                throw new InvalidOperationException(
                    "Cannot increment delayed retry count because the envelope has no headers collection.");
            }

            envelope.Headers.Set(MessageHeaders.Retry.DelayedRetryCount.Key, delayedRetryCount + 1);

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

    private static TimeSpan CalculateDelay(int attempt, RedeliveryPolicyConfig config)
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
            var jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
            baseDelay = TimeSpan.FromMilliseconds(Math.Max(0, baseDelay.TotalMilliseconds + jitter));
        }

        return baseDelay;
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                // Resolve exception policy feature from the most specific scope.
                var feature = context.GetExceptionPolicyFeature();

                if (feature is null)
                {
                    // No exception policy configured — skip redelivery middleware entirely.
                    return next;
                }

                var timeProvider = context.Services.GetRequiredService<TimeProvider>();
                var pools = context.Services.GetRequiredService<IMessagingPools>();

                var middleware = new ReceiveRedeliveryMiddleware(
                    feature.Rules,
                    timeProvider,
                    pools);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Redelivery");
}

file static class Extensions
{
    /// <summary>
    /// Resolves exception policy feature with the most specific scope taking precedence.
    /// Endpoint -> Transport -> Bus.
    /// </summary>
    public static ExceptionPolicyFeature? GetExceptionPolicyFeature(this ReceiveMiddlewareFactoryContext context)
    {
        var busFeatures = context.Services.GetRequiredService<IFeatureCollection>();

        // Endpoint -> Transport -> Bus (most specific first).
        if (context.Endpoint.Features.TryGet(out ExceptionPolicyFeature? endpointFeature))
        {
            return endpointFeature;
        }

        if (context.Transport.Features.TryGet(out ExceptionPolicyFeature? transportFeature))
        {
            return transportFeature;
        }

        if (busFeatures.TryGet(out ExceptionPolicyFeature? busFeature))
        {
            return busFeature;
        }

        return null;
    }
}
