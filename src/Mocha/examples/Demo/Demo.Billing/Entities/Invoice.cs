namespace Demo.Billing.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; }
    public required string CustomerId { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Refunded,
    Cancelled
}
