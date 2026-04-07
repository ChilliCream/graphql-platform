using System.Collections.Immutable;
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
    ImmutableArray<ExceptionPolicyRule> exceptionPolicyRules,
    TimeProvider timeProvider,
    IMessagingPools pools)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        // Read the current delayed retry count from headers.
        var delayedRetryCount = 0;

        if (context.Headers.TryGetValue(MessageHeaders.Retry.DelayedRetryCount.Key, out var headerValue))
        {
            delayedRetryCount = RedeliveryExecutor.ParseDelayedRetryCount(headerValue);
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

            var decision = RedeliveryExecutor.Evaluate(exceptionPolicyRules, ex, delayedRetryCount);

            switch (decision.Action)
            {
                case RedeliveryAction.Discard:
                    context.Features.GetOrSet<ReceiveConsumerFeature>().MessageConsumed = true;
                    return;

                case RedeliveryAction.Redeliver:
                    var scheduledTime = timeProvider.GetUtcNow().Add(decision.Delay);
                    await DispatchRedeliveryAsync(context, delayedRetryCount, scheduledTime);
                    context.Features.GetOrSet<ReceiveConsumerFeature>().MessageConsumed = true;
                    return;

                default:
                    throw;
            }
        }
    }

    private async ValueTask DispatchRedeliveryAsync(
        IReceiveContext context,
        int delayedRetryCount,
        DateTimeOffset scheduledTime)
    {
        var envelope = context.Envelope
            ?? throw new InvalidOperationException(
                "Cannot redeliver because the receive context has no envelope.");

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
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                // Resolve exception policy feature from the most specific scope.
                var feature = context.GetExceptionPolicyFeature();

                if (feature is null)
                {
                    // No exception policy configured - skip redelivery middleware entirely.
                    return next;
                }

                var timeProvider = context.Services.GetRequiredService<TimeProvider>();
                var pools = context.Services.GetRequiredService<IMessagingPools>();

                var middleware = new ReceiveRedeliveryMiddleware(feature.Rules.ToImmutableArray(), timeProvider, pools);

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
