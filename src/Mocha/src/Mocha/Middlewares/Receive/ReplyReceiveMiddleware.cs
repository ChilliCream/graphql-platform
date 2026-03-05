using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// A receive middleware that adds the reply consumer to the consumer list, enabling
/// request-reply message correlation on the receive endpoint.
/// </summary>
/// <param name="consumer">The reply consumer to register.</param>
public sealed class ReplyReceiveMiddleware(ReplyConsumer consumer)
{
    /// <summary>
    /// Executes the middleware, adding the reply consumer to the receive context before invoking the next delegate.
    /// </summary>
    /// <param name="context">The receive context.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        feature.Consumers.Add(consumer);

        await next(context);
    }

    /// <summary>
    /// Creates the middleware configuration for the reply receive middleware.
    /// </summary>
    /// <returns>A receive middleware configuration that creates the reply receive middleware.</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var replyConsumer = context
                    .Services.GetRequiredService<RegisteredConsumers>()
                    .Consumers.OfType<ReplyConsumer>()
                    .FirstOrDefault();

                if (replyConsumer == null)
                {
                    return next;
                }

                var instance = new ReplyReceiveMiddleware(replyConsumer);

                return ctx => instance.InvokeAsync(ctx, next);
            },
            "ReplyReceive");
}
