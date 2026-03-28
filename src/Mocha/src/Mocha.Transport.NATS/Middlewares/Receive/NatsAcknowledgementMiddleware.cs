using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.NATS.Features;

namespace Mocha.Transport.NATS.Middlewares;

/// <summary>
/// Receive middleware that acknowledges JetStream messages on success, negatively acknowledges on failure
/// for redelivery, and terminates delivery on poison messages that exceed the maximum retry count.
/// </summary>
internal sealed class NatsAcknowledgementMiddleware(int maxDeliver)
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and acknowledges or negatively acknowledges
    /// the JetStream message based on the outcome.
    /// </summary>
    /// <param name="context">The receive context containing the current message and features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<NatsReceiveFeature>();

        if (feature.Message is not { } msg)
        {
            throw new InvalidOperationException(
                "NatsReceiveFeature.Message is not set. "
                + "Ensure the acknowledgement middleware runs after the message is assigned.");
        }

        var cancellationToken = context.CancellationToken;

        try
        {
            await next(context);

            await msg.AckAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            var metadata = msg.Metadata;
            var deliveryCount = metadata is not null
                ? (int)Math.Min(metadata.Value.NumDelivered, int.MaxValue)
                : 0;

            if (deliveryCount >= maxDeliver)
            {
                await msg.AckTerminateAsync(cancellationToken: cancellationToken);
            }
            else
            {
                await msg.NakAsync(cancellationToken: cancellationToken);
            }

            throw;
        }
    }

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware,
    /// resolving the <c>MaxDeliver</c> setting from the endpoint's consumer at build time.
    /// </summary>
    /// <returns>A middleware configuration keyed as "NatsAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var maxDeliver = 5;

                if (context.Endpoint is NatsReceiveEndpoint natsEndpoint)
                {
                    maxDeliver = natsEndpoint.Consumer.MaxDeliver ?? 5;
                }

                var middleware = new NatsAcknowledgementMiddleware(maxDeliver);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "NatsAcknowledgement");
}
