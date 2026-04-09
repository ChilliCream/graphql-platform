using Mocha.Mediator;
using AzureEventHubTransport.Contracts.Mediator;

namespace AzureEventHubTransport.OrderService.Handlers;

/// <summary>
/// Mediator handler — returns the current status of an order.
/// In a real app this would query a database.
/// </summary>
public sealed class GetOrderStatusQueryHandler
    : IQueryHandler<GetOrderStatusQuery, OrderStatusResult>
{
    public ValueTask<OrderStatusResult> HandleAsync(
        GetOrderStatusQuery query,
        CancellationToken cancellationToken)
    {
        return new(new OrderStatusResult
        {
            OrderId = query.OrderId,
            Status = "Processing",
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
