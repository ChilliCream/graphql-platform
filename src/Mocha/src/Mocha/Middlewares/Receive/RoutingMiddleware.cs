using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Selects matching consumers for the resolved message type and current endpoint.
/// </summary>
/// <remarks>
/// Matches include enclosed message types so handlers registered for base contracts can receive
/// derived messages.
/// Without this middleware, no consumer list is built for execution and messages can traverse the
/// pipeline without ever reaching application handlers.
/// </remarks>
internal sealed class RoutingMiddleware(IMessageRouter router)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        if (context.MessageType is { } messageType)
        {
            var routes = router.GetInboundByEndpoint(context.Endpoint);

            foreach (var route in routes)
            {
                if (route.MessageType is not null
                    && route.Consumer is not null
                    && (
                        route.MessageType == messageType
                        || messageType.EnclosedMessageTypes.Contains(route.MessageType)))
                {
                    // Consumers are collected on the feature for later execution middleware.
                    feature.Consumers.Add(route.Consumer);
                }
            }
        }

        await next(context);
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var router = context.Services.GetRequiredService<IMessageRouter>();
                var middleware = new RoutingMiddleware(router);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "ConsumerSelection");
}
