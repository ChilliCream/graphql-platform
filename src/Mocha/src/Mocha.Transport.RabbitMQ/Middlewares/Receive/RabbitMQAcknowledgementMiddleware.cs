using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;

namespace Mocha.Transport.RabbitMQ.Middlewares;

/// <summary>
/// Receive middleware that sends a BasicAck on successful processing and a BasicNack (with requeue) on failure,
/// ensuring messages are properly acknowledged or returned to the broker.
/// </summary>
internal sealed class RabbitMQAcknowledgementMiddleware
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and acknowledges or negatively acknowledges the message based on the outcome.
    /// </summary>
    /// <param name="context">The receive context containing the current message and features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
        var channel = feature.Channel;
        var eventArgs = feature.EventArgs;
        var cancellationToken = context.CancellationToken;

        try
        {
            await next(context);

            if (channel.IsOpen)
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);
            }
        }
        catch
        {
            if (channel.IsOpen)
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true, cancellationToken);
            }

            throw;
        }
    }

    private static readonly RabbitMQAcknowledgementMiddleware _instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "RabbitMQAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) => ctx => _instance.InvokeAsync(ctx, next),
            "RabbitMQAcknowledgement");
}
