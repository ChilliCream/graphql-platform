using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Selects matching consumers for the current endpoint by evaluating each route's condition against
/// the received message.
/// </summary>
/// <remarks>
/// The default condition matches by message type, including enclosed message types so handlers
/// registered for base contracts can receive derived messages. Other conditions, such as header based
/// reply routing, select on envelope metadata alone.
/// Without this middleware, no consumer list is built for execution and messages can traverse the
/// pipeline without ever reaching application handlers.
/// </remarks>
internal sealed class RoutingMiddleware(IMessageRouter router)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        foreach (var route in router.GetInboundByEndpoint(context.Endpoint))
        {
            if (route.Consumer is not null && route.Condition.Matches(context))
            {
                // Consumers are collected on the feature for later execution middleware.
                feature.Consumers.Add(route.Consumer);
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
