namespace Demo.Contracts.Events;

/// <summary>
/// Published by Billing when payment processing fails.
/// </summary>
public sealed class PaymentFailedEvent
{
    public required Guid InvoiceId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset FailedAt { get; init; }
}
