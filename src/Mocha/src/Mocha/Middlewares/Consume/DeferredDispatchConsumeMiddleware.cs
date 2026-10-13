using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Middlewares;

/// <summary>
/// Holds the messages a consumer dispatches until the consumer and the middlewares inside it have
/// completed, then releases them in dispatch order. A failed attempt drops its messages.
/// </summary>
internal sealed class DeferredDispatchConsumeMiddleware(string consumerName)
{
    private readonly string _activityName = $"{consumerName} dispatch";

    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        var scope = context.Services.GetRequiredService<DeferredDispatchScope>();

        if (scope.IsOpen)
        {
            // An enclosing consumer attempt in the same scope owns the hold and releases it.
            await next(context);
            return;
        }

        scope.Open();

        try
        {
            await next(context);
            await FlushAsync(scope, context.CancellationToken);
        }
        catch
        {
            scope.Discard();
            throw;
        }
        finally
        {
            scope.Close();
        }
    }

    private async ValueTask FlushAsync(DeferredDispatchScope scope, CancellationToken cancellationToken)
    {
        if (!scope.HasPending)
        {
            return;
        }

        using var _ = OpenTelemetry.Source.StartActivity(_activityName, ActivityKind.Producer);

        await scope.FlushAsync(cancellationToken);
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var middleware = new DeferredDispatchConsumeMiddleware(context.Consumer.Name);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "DeferredDispatch");
}
