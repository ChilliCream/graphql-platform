using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Contracts.Events;
using Mocha;

namespace Demo.Billing.Handlers;

public class BulkOrderBatchHandler(BillingDbContext db, ILogger<BulkOrderBatchHandler> logger)
    : IBatchEventHandler<BulkOrderEvent>
{
    public async ValueTask HandleAsync(IMessageBatch<BulkOrderEvent> batch, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing BULK batch of {Count} orders (CompletionMode: {Mode})",
            batch.Count,
            batch.CompletionMode);

        var totalRevenue = 0m;
        var totalItems = 0;

        foreach (var order in batch)
        {
            totalRevenue += order.TotalAmount;
            totalItems += order.Quantity;
        }

        var summary = new RevenueSummary
        {
            Id = Guid.NewGuid(),
            OrderCount = batch.Count,
            TotalRevenue = totalRevenue,
            AverageOrderAmount = totalRevenue / batch.Count,
            TotalItemsSold = totalItems,
            PeriodStart = batch.GetContext(0).SentAt ?? DateTimeOffset.UtcNow,
            PeriodEnd = batch.GetContext(batch.Count - 1).SentAt ?? DateTimeOffset.UtcNow,
            CompletionMode = batch.CompletionMode.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.RevenueSummaries.Add(summary);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bulk revenue summary created: {SummaryId} - {OrderCount} orders, ${TotalRevenue:F2} total, mode={Mode}",
            summary.Id,
            summary.OrderCount,
            summary.TotalRevenue,
            summary.CompletionMode);
    }
}
