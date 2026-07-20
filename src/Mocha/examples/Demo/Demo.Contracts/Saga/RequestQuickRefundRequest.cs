using Mocha;

namespace Demo.Contracts.Saga;

/// <summary>
/// Request to initiate a quick refund (no physical return required).
/// Used for digital goods, low-value items, or goodwill refunds.
/// </summary>
public sealed class RequestQuickRefundRequest : IEventRequest<QuickRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string Reason { get; init; }
}
