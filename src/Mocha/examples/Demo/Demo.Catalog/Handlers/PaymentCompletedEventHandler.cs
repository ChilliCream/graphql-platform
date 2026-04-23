using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Catalog.Handlers;

public class PaymentCompletedEventHandler(CatalogDbContext db, ILogger<PaymentCompletedEventHandler> logger)
    : IEventHandler<PaymentCompletedEvent>
{
    public async ValueTask HandleAsync(PaymentCompletedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Payment completed for order {OrderId}, updating status to Paid", message.OrderId);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found", message.OrderId);
            return;
        }

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Order {OrderId} status updated to Paid", message.OrderId);
    }
}
