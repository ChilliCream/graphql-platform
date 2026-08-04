namespace Demo.Contracts.Events;

/// <summary>
/// Published by Billing as a scheduled reminder to check invoice payment status.
/// </summary>
public sealed class PaymentReminderEvent
{
    public required Guid InvoiceId { get; init; }
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
}
