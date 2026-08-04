using Mocha;

namespace Demo.Contracts.Commands;

/// <summary>
/// Command to create a return shipping label.
/// </summary>
public sealed class CreateReturnLabelCommand : IEventRequest<CreateReturnLabelResponse>
{
    public required Guid OrderId { get; init; }
    public required Guid OriginalShipmentId { get; init; }
    public required string CustomerAddress { get; init; }
    public required string CustomerId { get; init; }

    // Order details needed for saga when package arrives
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Response from creating a return label.
/// </summary>
public sealed class CreateReturnLabelResponse
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
    public required bool Success { get; init; }
    public string? ReturnTrackingNumber { get; init; }
    public string? ReturnLabelUrl { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
