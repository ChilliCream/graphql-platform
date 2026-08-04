using Mocha;

namespace Demo.Contracts.Commands;

/// <summary>
/// Command to inspect a returned item.
/// </summary>
public sealed class InspectReturnCommand : IEventRequest<InspectReturnResponse>
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
    public required Guid ReturnId { get; init; }
}

/// <summary>
/// Response from inspecting a returned item.
/// </summary>
public sealed class InspectReturnResponse
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid ReturnId { get; init; }
    public required bool Passed { get; init; }
    public required InspectionResult Result { get; init; }
    public string? Notes { get; init; }
    public required DateTimeOffset InspectedAt { get; init; }
}

/// <summary>
/// Result of return inspection.
/// </summary>
public enum InspectionResult
{
    /// <summary>Item is in good condition, can be restocked.</summary>
    Passed,

    /// <summary>Item is damaged by customer, partial refund only.</summary>
    DamagedByCustomer,

    /// <summary>Item is defective (manufacturer issue), full refund.</summary>
    Defective,

    /// <summary>Wrong item returned.</summary>
    WrongItem
}
