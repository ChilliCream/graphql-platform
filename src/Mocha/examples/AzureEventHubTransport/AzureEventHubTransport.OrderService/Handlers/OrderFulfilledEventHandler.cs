using Mocha;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.OrderService.Handlers;

/// <summary>
/// Bus event handler — logs that the full order lifecycle is complete.
/// The saga publishes <see cref="OrderFulfilledEvent"/> at the end.
/// </summary>
public sealed class OrderFulfilledEventHandler(ILogger<OrderFulfilledEventHandler> logger)
    : IEventHandler<OrderFulfilledEvent>
{
    public ValueTask HandleAsync(OrderFulfilledEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} fulfilled — payment {PaymentId}, shipped via {Carrier} ({TrackingNumber})",
            message.OrderId,
            message.PaymentId,
            message.Carrier,
            message.TrackingNumber);

        return ValueTask.CompletedTask;
    }
}
