using Mocha.Mediator;

namespace AzureEventHubTransport.Contracts.Mediator;

/// <summary>
/// Local query to retrieve the current status of an order.
/// </summary>
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
