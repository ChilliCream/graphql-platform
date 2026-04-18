using AotExample.Contracts.Events;
using AotExample.Contracts.Requests;
using Mocha;

namespace AotExample.FulfillmentService.Handlers;

public sealed class OrderPlacedHandler(IMessageBus messageBus, ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogOrderReceived(message.OrderId, message.Quantity, message.ProductName);

        // Check inventory with OrderService via bus request/response
        var inventory = await messageBus.RequestAsync(
            new CheckInventoryRequest { ProductName = message.ProductName, Quantity = message.Quantity },
            cancellationToken);

        if (!inventory.IsAvailable)
        {
            logger.LogCannotFulfillOrder(message.OrderId, inventory.QuantityOnHand, message.ProductName);
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

        logger.LogOrderFulfilled(message.OrderId, trackingNumber);
    }
}

internal static partial class Logs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received order {OrderId}: {Quantity}x {ProductName}")]
    public static partial void LogOrderReceived(this ILogger logger, string orderId, int quantity, string productName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot fulfill order {OrderId}: only {QuantityOnHand} of {ProductName} on hand")]
    public static partial void LogCannotFulfillOrder(this ILogger logger, string orderId, int quantityOnHand, string productName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} fulfilled - tracking {TrackingNumber}")]
    public static partial void LogOrderFulfilled(this ILogger logger, string orderId, string trackingNumber);
}
