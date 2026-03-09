using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Drops messages that passed their <c>DeliverBy</c> timestamp before any costly deserialization or
/// handler work runs.
/// </summary>
/// <remarks>
/// Without this guard, stale commands/events can still mutate state after their validity window,
/// and expired backlog continues to consume processing capacity.
/// </remarks>
internal sealed class ReceiveExpiryMiddleware(TimeProvider timeProvider)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        if (context.DeliverBy.HasValue && context.DeliverBy.Value < timeProvider.GetUtcNow())
        {
            // Expired messages are considered settled to avoid retries of work that is no longer valid.
            feature.MessageConsumed = true;

            return;
        }

        await next(context);
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var timeProvider = context.Services.GetRequiredService<TimeProvider>();
                var middleware = new ReceiveExpiryMiddleware(timeProvider);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Expiry");
}
