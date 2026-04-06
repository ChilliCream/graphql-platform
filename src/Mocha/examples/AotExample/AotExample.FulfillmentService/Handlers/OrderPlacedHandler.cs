using AotExample.Contracts.Events;
using AotExample.Contracts.Requests;
using Mocha;

namespace AotExample.FulfillmentService.Handlers;

public sealed class OrderPlacedHandler(IMessageBus messageBus, ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received order {OrderId}: {Quantity}x {ProductName}",
            message.OrderId,
            message.Quantity,
            message.ProductName);

        // Check inventory with OrderService via bus request/response
        var inventory = await messageBus.RequestAsync(
            new CheckInventoryRequest { ProductName = message.ProductName, Quantity = message.Quantity },
            cancellationToken);

        if (!inventory.IsAvailable)
        {
            logger.LogWarning(
                "Cannot fulfill order {OrderId}: only {QuantityOnHand} of {ProductName} on hand",
                message.OrderId,
                inventory.QuantityOnHand,
                message.ProductName);
            return;
        }

        var trackingNumber = $"TRK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = message.OrderId,
                TrackingNumber = trackingNumber,
                CorrelationId = message.CorrelationId
            },
            cancellationToken);

        logger.LogInformation("Order {OrderId} fulfilled — tracking {TrackingNumber}", message.OrderId, trackingNumber);
    }
}
