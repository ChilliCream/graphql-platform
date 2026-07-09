using Mocha;

namespace Demo.Contracts.Commands;

/// <summary>
/// Command to process a partial refund (e.g., when returned item is damaged).
/// </summary>
public sealed class ProcessPartialRefundCommand : IEventRequest<ProcessPartialRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal OriginalAmount { get; init; }
    public required decimal RefundPercentage { get; init; }
    public required string Reason { get; init; }
    public required string CustomerId { get; init; }
}

/// <summary>
/// Response from processing a partial refund.
/// </summary>
public sealed class ProcessPartialRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal OriginalAmount { get; init; }
    public required decimal RefundedAmount { get; init; }
    public required decimal RefundPercentage { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
