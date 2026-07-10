using Mocha;

namespace Demo.Contracts.Saga;

/// <summary>
/// Request to initiate a full return process.
/// Creates a return label, waits for package, inspects, and processes refund.
/// </summary>
public sealed class InitiateReturnRequest : IEventRequest<ReturnProcessingResponse>
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string CustomerAddress { get; init; }
    public required Guid OriginalShipmentId { get; init; }
    public required string Reason { get; init; }
}
