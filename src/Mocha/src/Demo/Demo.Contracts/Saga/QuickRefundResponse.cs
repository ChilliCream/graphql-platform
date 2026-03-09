namespace Demo.Contracts.Saga;

/// <summary>
/// Response from the Quick Refund saga.
/// </summary>
public sealed class QuickRefundResponse
{
    public required Guid OrderId { get; init; }
    public required bool Success { get; init; }
    public Guid? RefundId { get; init; }
    public decimal? RefundedAmount { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}
