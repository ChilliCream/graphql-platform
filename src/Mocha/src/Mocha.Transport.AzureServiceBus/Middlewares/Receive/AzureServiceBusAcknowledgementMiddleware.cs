using Azure.Messaging.ServiceBus;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Features;

namespace Mocha.Transport.AzureServiceBus.Middlewares;

/// <summary>
/// Receive middleware that completes messages on successful processing and abandons them on failure,
/// ensuring messages are properly acknowledged or returned to the broker. Operates uniformly on
/// session and non-session endpoints by going through <see cref="IAzureServiceBusMessageActions"/>.
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
        var actions = context.Features.GetOrSet<AzureServiceBusReceiveFeature>().Actions;
        var cancellationToken = context.CancellationToken;

        try
        {
            await next(context);

            await CompleteAsync(actions, cancellationToken);
        }
        catch
        {
            try
            {
                await AbandonAsync(actions, cancellationToken);
            }
            catch
            {
                // Abandon failure is secondary — the original exception is more important.
            }

            throw;
        }
    }

    private static async Task CompleteAsync(IAzureServiceBusMessageActions actions, CancellationToken cancellationToken)
    {
        try
        {
            await actions.CompleteAsync(cancellationToken);
        }
        catch (ServiceBusException ex) when (IsLockLost(ex))
        {
            // Message was already settled (e.g. handler called DeadLetterAsync directly via
            // IAzureServiceBusMessageActions, or the lock expired). Treat as already-settled.
        }
    }

    private static async Task AbandonAsync(IAzureServiceBusMessageActions actions, CancellationToken cancellationToken)
    {
        try
        {
            await actions.AbandonAsync(cancellationToken: cancellationToken);
        }
        catch (ServiceBusException ex) when (IsLockLost(ex))
        {
            // Message was already settled or the lock expired. Nothing else to do.
        }
    }

    private static bool IsLockLost(ServiceBusException ex)
        => ex.Reason is ServiceBusFailureReason.MessageLockLost
            or ServiceBusFailureReason.SessionLockLost;

    private static readonly AzureServiceBusAcknowledgementMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "AzureServiceBusAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (_, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "AzureServiceBusAcknowledgement");
}
