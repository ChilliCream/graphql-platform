using Mocha.Mediator;

namespace KafkaTransport.OrderService.Mediator.Queries;

public sealed class GetOrderStatusQueryHandler
    : IQueryHandler<GetOrderStatusQuery, OrderStatusResult>
{
    public ValueTask<OrderStatusResult> HandleAsync(
        GetOrderStatusQuery query,
        CancellationToken cancellationToken)
    {
        // In a real app this would query a database or the saga store
        return new(new OrderStatusResult
        {
            OrderId = query.OrderId,
            Status = "Processing",
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
