using Mocha;
using PostgresTransport.Contracts.Events;

namespace PostgresTransport.ShippingService.Handlers;

public sealed class OrderPlacedEventHandler(
    IMessageBus messageBus,
    ILogger<OrderPlacedEventHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    private static readonly string[] s_carriers = ["FedEx", "UPS", "DHL", "USPS"];

    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Preparing shipment for order {OrderId}: {Quantity}x {ProductName} (${TotalAmount}) → {CustomerEmail}",
            message.OrderId,
            message.Quantity,
            message.ProductName,
            message.TotalAmount,
            message.CustomerEmail);

        // Simulate shipping processing time
        await Task.Delay(500, cancellationToken);

        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var carrier = s_carriers[Random.Shared.Next(s_carriers.Length)];

        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = message.OrderId,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                ShippedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        logger.LogInformation(
            "Order {OrderId} shipped via {Carrier}, tracking: {TrackingNumber}",
            message.OrderId, carrier, trackingNumber);
    }
}
