namespace Demo.Billing.Entities;

public class Refund
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal RefundPercentage { get; set; }
    public required string CustomerId { get; set; }
    public required string Reason { get; set; }
    public RefundStatus Status { get; set; }
    public RefundType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public enum RefundStatus
{
    Pending,
    Completed,
    Failed
}

public enum RefundType
{
    Full,
    Partial
}
