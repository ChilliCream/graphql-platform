namespace Demo.Billing.Entities;

public class RevenueSummary
{
    public Guid Id { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public int TotalItemsSold { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public required string CompletionMode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
