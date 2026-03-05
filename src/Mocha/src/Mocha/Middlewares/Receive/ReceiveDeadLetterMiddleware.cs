using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Final receive-pipeline safety net that guarantees unconsumed messages are forwarded to the
/// endpoint-specific error endpoint.
/// </summary>
/// <remarks>
/// Exceptions from downstream middleware are swallowed after logging because dead-lettering is the
/// terminal reliability behavior for this pipeline branch.
/// Without this middleware, poison/unhandled messages can stay in the normal receive flow and be
/// repeatedly retried, wasting throughput and making the failure harder to diagnose and recover.
/// </remarks>
internal sealed class ReceiveDeadLetterMiddleware(
    DispatchEndpoint errorEndpoint,
    IMessagingPools pools,
    ILogger<ReceiveDeadLetterMiddleware> logger)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.ExceptionOccurred(ex);
        }

        if (!feature.MessageConsumed)
        {
            // Re-dispatch the original envelope as-is so diagnostics and payload stay intact.
            var dispatchContext = pools.DispatchContext.Get();
            try
            {
                dispatchContext.Initialize(
                    context.Services,
                    errorEndpoint,
                    context.Runtime,
                    context.MessageType,
                    context.CancellationToken);

                dispatchContext.Envelope = context.Envelope;

                await errorEndpoint.ExecuteAsync(dispatchContext);
            }
            finally
            {
                pools.DispatchContext.Return(dispatchContext);
            }

            // Mark consumed to prevent duplicate settlement/forwarding decisions downstream.
            feature.MessageConsumed = true;
        }
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var errorEndpoint = context.Endpoint.ErrorEndpoint;
                if (errorEndpoint is null)
                {
                    return next;
                }

                var pools = context.Services.GetRequiredService<IMessagingPools>();
                var logger = context.Services.GetRequiredService<ILogger<ReceiveDeadLetterMiddleware>>();
                var middleware = new ReceiveDeadLetterMiddleware(errorEndpoint, pools, logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "DeadLetter");
}

internal static partial class ReceiveDeadLetterMiddlewareLogs
{
    [LoggerMessage(
        LogLevel.Critical,
        "An exception occurred while processing the message. The message will be moved to the error endpoint.")]
    public static partial void ExceptionOccurred(this ILogger<ReceiveDeadLetterMiddleware> logger, Exception ex);
}
