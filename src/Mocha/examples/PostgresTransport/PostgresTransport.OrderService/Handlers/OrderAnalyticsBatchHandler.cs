using Mocha;
using PostgresTransport.Contracts.Events;

namespace PostgresTransport.OrderService.Handlers;

public sealed class OrderAnalyticsBatchHandler(ILogger<OrderAnalyticsBatchHandler> logger)
    : IBatchEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(IMessageBatch<OrderPlacedEvent> batch, CancellationToken cancellationToken)
    {
        var totalRevenue = batch.Sum(e => e.TotalAmount);
        var uniqueCustomers = batch.Select(e => e.CustomerEmail).Distinct().Count();

        logger.LogInformation(
            "Batch analytics: {Count} orders (mode: {Mode}), revenue: ${Revenue:F2}, customers: {Customers}",
            batch.Count,
            batch.CompletionMode,
            totalRevenue,
            uniqueCustomers);

        return ValueTask.CompletedTask;
    }
}
