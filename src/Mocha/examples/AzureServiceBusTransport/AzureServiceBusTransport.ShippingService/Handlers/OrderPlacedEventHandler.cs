using Mocha;
using AzureServiceBusTransport.Contracts.Events;

namespace AzureServiceBusTransport.ShippingService.Handlers;

public sealed class OrderPlacedEventHandler(
    IMessageBus messageBus,
    ILogger<OrderPlacedEventHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    private static readonly string[] Carriers = ["FedEx", "UPS", "DHL", "USPS"];

    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Preparing shipment for order {OrderId}: {Quantity}x {ProductName} (${TotalAmount}) -> {CustomerEmail}",
            message.OrderId,
            message.Quantity,
            message.ProductName,
            message.TotalAmount,
            message.CustomerEmail);

        // Simulate shipping processing time
        await Task.Delay(500, cancellationToken);

        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var carrier = Carriers[Random.Shared.Next(Carriers.Length)];

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
