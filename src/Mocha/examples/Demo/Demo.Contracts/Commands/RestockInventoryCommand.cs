using Mocha;

namespace Demo.Contracts.Commands;

/// <summary>
/// Command to restock inventory after a return is accepted.
/// </summary>
public sealed class RestockInventoryCommand : IEventRequest<RestockInventoryResponse>
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
    public required Guid ReturnId { get; init; }
}

/// <summary>
/// Response from restocking inventory.
/// </summary>
public sealed class RestockInventoryResponse
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int QuantityRestocked { get; init; }
    public required int NewStockLevel { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset RestockedAt { get; init; }
}
