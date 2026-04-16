using Demo.Contracts.Events;
using Demo.Contracts.Requests;
using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Mocha;

namespace Demo.Shipping.Handlers;

public class PaymentCompletedEventHandler(
    ShippingDbContext db,
    IMessageBus messageBus,
    ILogger<PaymentCompletedEventHandler> logger) : IEventHandler<PaymentCompletedEvent>
{
    public async ValueTask HandleAsync(PaymentCompletedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Payment completed for order {OrderId}, creating shipment", message.OrderId);

        // Request product details from Catalog service
        var productResponse = await messageBus.RequestAsync(
            new GetProductRequest { ProductId = Guid.Empty }, // We'd need the product ID from the order
            cancellationToken);

        // Generate tracking number
        var trackingNumber = $"TRK-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        // Create shipment
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Address = "Address from order", // In real app, we'd get this from the order
            Status = ShipmentStatus.Processing,
            TrackingNumber = trackingNumber,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Shipment {ShipmentId} created for order {OrderId} with tracking {TrackingNumber}",
            shipment.Id,
            message.OrderId,
            trackingNumber);

        // Publish ShipmentCreatedEvent
        await messageBus.PublishAsync(
            new ShipmentCreatedEvent
            {
                ShipmentId = shipment.Id,
                OrderId = message.OrderId,
                Address = shipment.Address,
                TrackingNumber = trackingNumber,
                CreatedAt = shipment.CreatedAt
            },
            cancellationToken);

        logger.LogInformation("ShipmentCreatedEvent published for order {OrderId}", message.OrderId);
    }
}
