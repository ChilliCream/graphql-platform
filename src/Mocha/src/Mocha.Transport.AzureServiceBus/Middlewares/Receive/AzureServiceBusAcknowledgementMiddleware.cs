using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var feature = context.Features.GetOrSet<AzureServiceBusReceiveFeature>();
        var actions = feature.Actions;
        var message = feature.Message;
        var entityPath = feature.ProcessMessageEventArgs?.EntityPath
            ?? feature.ProcessSessionMessageEventArgs?.EntityPath
            ?? string.Empty;
        var cancellationToken = context.CancellationToken;

        try
        {
            await next(context);

            await CompleteAsync(actions, context.Services, message, entityPath, cancellationToken);
        }
        catch
        {
            try
            {
                await AbandonAsync(actions, context.Services, message, entityPath, cancellationToken);
            }
            catch
            {
                // Abandon failure is secondary, the original exception is more important.
            }

            throw;
        }
    }

    internal static async Task CompleteAsync(
        IAzureServiceBusMessageActions actions,
        IServiceProvider services,
        ServiceBusReceivedMessage message,
        string entityPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await actions.CompleteAsync(cancellationToken);
        }
        catch (ServiceBusException ex) when (IsLockLost(ex))
        {
            services.GetRequiredService<ILogger<AzureServiceBusAcknowledgementMiddleware>>()
                .AcknowledgementLockLost(
                    "Complete",
                    entityPath,
                    message.MessageId,
                    message.SessionId,
                    ex.Reason,
                    ex);

            // Two cases collapse into MessageLockLost/SessionLockLost here:
            //   1. The handler already settled the message itself (e.g. called DeadLetterAsync
            //      or DeferAsync via IAzureServiceBusMessageActions). Completing again is a
            //      no-op from the broker's perspective, the work is done.
            //   2. The peek-lock or session lock expired before we reached this point (slow
            //      handler exceeded LockDuration). The broker has already returned the message
            //      to the queue and it will be redelivered to another consumer. There is
            //      nothing we can do to settle it now; throwing would only mask the successful
            //      handler invocation.
            // Both cases are compatible with the at-least-once contract, so swallow.
        }
    }

    internal static async Task AbandonAsync(
        IAzureServiceBusMessageActions actions,
        IServiceProvider services,
        ServiceBusReceivedMessage message,
        string entityPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await actions.AbandonAsync(cancellationToken: cancellationToken);
        }
        catch (ServiceBusException ex) when (IsLockLost(ex))
        {
            services.GetRequiredService<ILogger<AzureServiceBusAcknowledgementMiddleware>>()
                .AcknowledgementLockLost(
                    "Abandon",
                    entityPath,
                    message.MessageId,
                    message.SessionId,
                    ex.Reason,
                    ex);

            // Same two cases as in CompleteAsync: the handler settled the message before
            // throwing, or the lock expired while the handler was running. In the lock-expired
            // case the broker has already requeued the message for redelivery, which is exactly
            // what abandoning would have done, so there is no recovery action to take.
        }
    }

    private static bool IsLockLost(ServiceBusException ex)
        => ex.Reason is ServiceBusFailureReason.MessageLockLost or ServiceBusFailureReason.SessionLockLost;

    private static readonly AzureServiceBusAcknowledgementMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "AzureServiceBusAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "AzureServiceBusAcknowledgement");
}

internal static partial class AcknowledgementLogs
{
    [LoggerMessage(
        LogLevel.Warning,
        "Azure Service Bus lock lost during {Operation} settlement on entity {EntityPath} " +
        "for message {MessageId} (SessionId: {SessionId}, Reason: {Reason}); message settlement skipped")]
    public static partial void AcknowledgementLockLost(
        this ILogger logger,
        string operation,
        string entityPath,
        string? messageId,
        string? sessionId,
        ServiceBusFailureReason reason,
        Exception exception);
}
