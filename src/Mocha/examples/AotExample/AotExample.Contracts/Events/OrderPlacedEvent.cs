namespace AotExample.Contracts.Events;

public sealed class OrderPlacedEvent
{
    public required string OrderId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
}
