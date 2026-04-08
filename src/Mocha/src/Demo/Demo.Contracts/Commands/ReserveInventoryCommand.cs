namespace Demo.Contracts.Commands;

/// <summary>
/// Command to reserve inventory for an order.
/// </summary>
public sealed class ReserveInventoryCommand
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}
