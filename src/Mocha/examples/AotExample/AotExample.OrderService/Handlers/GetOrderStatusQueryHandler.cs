using AotExample.OrderService.Queries;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class GetOrderStatusQueryHandler
    : IQueryHandler<GetOrderStatusQuery, GetOrderStatusResponse>
{
    public ValueTask<GetOrderStatusResponse> HandleAsync(
        GetOrderStatusQuery query,
        CancellationToken cancellationToken)
    {
        return new ValueTask<GetOrderStatusResponse>(
            new GetOrderStatusResponse
            {
                OrderId = query.OrderId,
                Status = "Processing"
            });
    }
}
