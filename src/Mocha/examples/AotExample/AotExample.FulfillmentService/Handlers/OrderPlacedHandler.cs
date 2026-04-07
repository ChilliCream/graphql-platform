using AotExample.Contracts.Events;
using AotExample.Contracts.Requests;
using Mocha;

namespace AotExample.FulfillmentService.Handlers;

public sealed partial class OrderPlacedHandler(IMessageBus messageBus, ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        LogOrderReceived(message.OrderId, message.Quantity, message.ProductName);

        // Check inventory with OrderService via bus request/response
        var inventory = await messageBus.RequestAsync(
            new CheckInventoryRequest { ProductName = message.ProductName, Quantity = message.Quantity },
            cancellationToken);

        if (!inventory.IsAvailable)
        {
            LogCannotFulfillOrder(message.OrderId, inventory.QuantityOnHand, message.ProductName);
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

        LogOrderFulfilled(message.OrderId, trackingNumber);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Received order {OrderId}: {Quantity}x {ProductName}")]
    private partial void LogOrderReceived(string orderId, int quantity, string productName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot fulfill order {OrderId}: only {QuantityOnHand} of {ProductName} on hand")]
    private partial void LogCannotFulfillOrder(string orderId, int quantityOnHand, string productName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} fulfilled — tracking {TrackingNumber}")]
    private partial void LogOrderFulfilled(string orderId, string trackingNumber);
}
