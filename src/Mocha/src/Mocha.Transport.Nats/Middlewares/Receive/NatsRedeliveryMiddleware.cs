using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Features;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats.Middlewares;

/// <summary>
/// Redelivers a failed JetStream message by negatively acknowledging it with the policy's delay, so
/// the server returns it to the same consumer and no other.
/// </summary>
/// <remarks>
/// Replaces the core redelivery middleware on NATS endpoints. The redelivery count is the server's
/// delivery count less one, so every delivery counts, including one that timed out.
/// </remarks>
internal sealed class NatsRedeliveryMiddleware(ImmutableArray<ExceptionPolicyRule> rules)
{
    /// <summary>
    /// Runs the pipeline and redelivers, discards or rethrows a failure as the exception policy decides.
    /// </summary>
    /// <param name="context">The receive context.</param>
    /// <param name="next">The next middleware.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<NatsReceiveFeature>();

        if (feature.Message is not { } message)
        {
            // Core NATS delivery, as used by reply endpoints: nothing to redeliver.
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Request/reply messages must not be redelivered - the caller is waiting.
            if (context.Envelope?.ResponseAddress is not null)
            {
                throw;
            }

            var redeliveries = Math.Max(feature.DeliveryCount - 1, 0);
            var decision = RedeliveryExecutor.Evaluate(rules, ex, redeliveries);

            switch (decision.Action)
            {
                case RedeliveryAction.Discard:
                    context.Features.GetOrSet<ReceiveConsumerFeature>().MessageConsumed = true;
                    return;

                case RedeliveryAction.Redeliver:
                    await message.NakAsync(
                        new AckOpts { NakDelay = decision.Delay },
                        CancellationToken.None);
                    feature.Settled = true;
                    context.Features.GetOrSet<ReceiveConsumerFeature>().MessageConsumed = true;
                    return;

                default:
                    throw;
            }
        }
    }

    /// <summary>
    /// Creates the middleware configuration, keyed like the core redelivery middleware it replaces.
    /// </summary>
    /// <returns>The configuration.</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                if (ResolveExceptionPolicy(context) is not { } feature)
                {
                    return next;
                }

                var middleware = new NatsRedeliveryMiddleware([.. feature.Rules]);

                return ctx => middleware.InvokeAsync(ctx, next);
            },
            ReceiveMiddlewares.Redelivery.Key);

    private static ExceptionPolicyFeature? ResolveExceptionPolicy(ReceiveMiddlewareFactoryContext context)
    {
        if (context.Endpoint.Features.TryGet(out ExceptionPolicyFeature? endpointFeature))
        {
            return endpointFeature;
        }

        if (context.Transport.Features.TryGet(out ExceptionPolicyFeature? transportFeature))
        {
            return transportFeature;
        }

        return context.Services.GetRequiredService<IFeatureCollection>().TryGet(out ExceptionPolicyFeature? busFeature)
            ? busFeature
            : null;
    }
}
