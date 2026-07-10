using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Catalog.Handlers;

public class ShipmentCreatedEventHandler(CatalogDbContext db, ILogger<ShipmentCreatedEventHandler> logger)
    : IEventHandler<ShipmentCreatedEvent>
{
    public async ValueTask HandleAsync(ShipmentCreatedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Shipment created for order {OrderId}, updating status to Shipping", message.OrderId);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found", message.OrderId);
            return;
        }

        order.Status = OrderStatus.Shipping;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} status updated to Shipping with tracking {TrackingNumber}",
            message.OrderId,
            message.TrackingNumber);
    }
}
