using Mocha.Mediator;

namespace AzureEventHubTransport.Contracts.Mediator;

/// <summary>
/// Local command to create an order, validate inputs, and persist it.
/// Handled in-process by the mediator before publishing to the message bus.
/// </summary>
public sealed class CreateOrderCommand : ICommand<CreateOrderResult>
{
    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public required string CustomerEmail { get; init; }
}

public sealed class CreateOrderResult
{
    public required Guid OrderId { get; init; }

    public required decimal TotalAmount { get; init; }
}
