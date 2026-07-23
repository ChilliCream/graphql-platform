using Mocha.Mediator;

namespace AzureServiceBusTransport.OrderService.Mediator.Queries;

public sealed class GetOrderDetailsQuery : IQuery<OrderDetails>
{
    public required Guid OrderId { get; init; }
}

public sealed class OrderDetails
{
    public required Guid OrderId { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset LastUpdated { get; init; }
}
