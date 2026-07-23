using Mocha.Mediator;

namespace AzureServiceBusTransport.OrderService.Mediator.Queries;

/// <summary>
/// In-process query handler that returns order details.
/// In a real application this would query a database.
/// </summary>
public sealed class GetOrderDetailsQueryHandler
    : IQueryHandler<GetOrderDetailsQuery, OrderDetails>
{
    public ValueTask<OrderDetails> HandleAsync(
        GetOrderDetailsQuery query,
        CancellationToken cancellationToken)
    {
        return new(new OrderDetails
        {
            OrderId = query.OrderId,
            Status = "Processing",
            LastUpdated = DateTimeOffset.UtcNow
        });
    }
}
