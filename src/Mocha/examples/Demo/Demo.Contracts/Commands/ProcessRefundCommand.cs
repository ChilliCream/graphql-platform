using Mocha;

namespace Demo.Contracts.Commands;

/// <summary>
/// Command to process a full refund for an order.
/// </summary>
public sealed class ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required string CustomerId { get; init; }
}

/// <summary>
/// Response from processing a refund.
/// </summary>
public sealed class ProcessRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
