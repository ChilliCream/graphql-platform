using Mocha.Mediator;

namespace KafkaTransport.OrderService.Mediator.Queries;

public sealed class GetOrderStatusQuery : IQuery<OrderStatusResult>
{
    public required Guid OrderId { get; init; }
}

public sealed class OrderStatusResult
{
    public required Guid OrderId { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
