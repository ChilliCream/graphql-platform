using Mocha.Mediator;

namespace AotExample.Contracts.Commands;

public sealed class PlaceOrderCommand : ICommand<PlaceOrderResult>
{
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
}

public sealed class PlaceOrderResult
{
    public required string OrderId { get; init; }
}
