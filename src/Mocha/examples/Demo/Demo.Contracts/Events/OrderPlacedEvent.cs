namespace Demo.Contracts.Events;

/// <summary>
/// Published by Catalog when a new order is placed.
/// </summary>
public sealed class OrderPlacedEvent
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string CustomerId { get; init; }
    public required string ShippingAddress { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
