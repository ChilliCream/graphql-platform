using Mocha.Mediator;

namespace AotExample.OrderService.Queries;

public sealed class GetOrderStatusQuery : IQuery<GetOrderStatusResponse>
{
    public required string OrderId { get; init; }
}
