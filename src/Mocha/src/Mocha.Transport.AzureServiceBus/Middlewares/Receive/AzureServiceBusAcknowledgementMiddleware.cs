using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus.Middlewares;

/// <summary>
/// Receive middleware that completes messages on successful processing and abandons them on failure,
/// ensuring messages are properly acknowledged or returned to the broker.
/// </summary>
internal sealed class AzureServiceBusAcknowledgementMiddleware
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and settles the message based on the outcome.
    /// </summary>
    /// <param name="context">The receive context containing the current message and features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<AzureServiceBusReceiveFeature>();
        var args = feature.ProcessMessageEventArgs;
        var cancellationToken = context.CancellationToken;

        try
        {
            await next(context);

            await args.CompleteMessageAsync(args.Message, cancellationToken);
        }
        catch
        {
            try
            {
                await args.AbandonMessageAsync(args.Message, cancellationToken: cancellationToken);
            }
            catch
            {
                // Abandon failure is secondary — the original exception is more important.
            }

            throw;
        }
    }

    private static readonly AzureServiceBusAcknowledgementMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "AzureServiceBusAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "AzureServiceBusAcknowledgement");
}
