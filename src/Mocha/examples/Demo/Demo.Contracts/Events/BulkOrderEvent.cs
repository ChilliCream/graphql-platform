namespace Demo.Contracts.Events;

/// <summary>
/// Lightweight event for high-volume batch processing demos.
/// Unlike OrderPlacedEvent, this has no per-message handler - only a batch handler
/// with MaxBatchSize=500 processes these events.
/// </summary>
public sealed class BulkOrderEvent
{
    public required Guid OrderId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string CustomerId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
