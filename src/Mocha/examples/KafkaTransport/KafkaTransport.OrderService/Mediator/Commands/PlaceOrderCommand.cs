using Mocha.Mediator;

namespace KafkaTransport.OrderService.Mediator.Commands;

public sealed class PlaceOrderCommand : ICommand<PlaceOrderResult>
{
    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public required string CustomerEmail { get; init; }
}

public sealed class PlaceOrderResult
{
    public required Guid OrderId { get; init; }

    public required string Status { get; init; }
}
