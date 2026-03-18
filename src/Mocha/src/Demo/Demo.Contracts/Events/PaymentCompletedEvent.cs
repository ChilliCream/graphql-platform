namespace Demo.Contracts.Events;

/// <summary>
/// Published by Billing after successful payment processing.
/// </summary>
public sealed class PaymentCompletedEvent
{
    public required Guid PaymentId { get; init; }
    public required Guid InvoiceId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string PaymentMethod { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
