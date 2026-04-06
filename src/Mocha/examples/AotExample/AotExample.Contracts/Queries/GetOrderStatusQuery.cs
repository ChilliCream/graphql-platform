using Mocha.Mediator;

namespace AotExample.Contracts.Queries;

public sealed class GetOrderStatusQuery : IQuery<GetOrderStatusResponse>
{
    public required string OrderId { get; init; }
}
