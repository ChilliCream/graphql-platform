namespace Demo.Contracts.Events;

/// <summary>
/// Published by Shipping when a return package arrives at the warehouse.
/// This event starts the ReturnProcessingSaga for inspection and refund.
/// </summary>
public sealed class ReturnPackageReceivedEvent
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }

    // Order details needed for saga processing
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public string? Reason { get; init; }
}
