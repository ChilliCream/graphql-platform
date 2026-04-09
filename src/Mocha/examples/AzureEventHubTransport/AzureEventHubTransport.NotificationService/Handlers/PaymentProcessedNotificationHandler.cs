using Mocha;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.NotificationService.Handlers;

public sealed class PaymentProcessedNotificationHandler(ILogger<PaymentProcessedNotificationHandler> logger)
    : IEventHandler<PaymentProcessedEvent>
{
    public ValueTask HandleAsync(PaymentProcessedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[EMAIL] Payment receipt sent for order {OrderId} — payment {PaymentId}, success: {Success}",
            message.OrderId,
            message.PaymentId,
            message.Success);

        return ValueTask.CompletedTask;
    }
}
