namespace Demo.Contracts.Saga;

/// <summary>
/// Response from the Return Processing saga.
/// </summary>
public sealed class ReturnProcessingResponse
{
    public required Guid OrderId { get; init; }
    public required bool Success { get; init; }
    public required ReturnOutcome Outcome { get; init; }

    // Return label info
    public Guid? ReturnId { get; init; }
    public string? ReturnTrackingNumber { get; init; }

    // Refund info
    public Guid? RefundId { get; init; }
    public decimal? RefundedAmount { get; init; }
    public decimal? RefundPercentage { get; init; }

    // Restock info
    public bool InventoryRestocked { get; init; }
    public int? QuantityRestocked { get; init; }

    // Failure info
    public string? FailureReason { get; init; }
    public string? FailureStage { get; init; }

    public required DateTimeOffset CompletedAt { get; init; }
}

/// <summary>
/// Possible outcomes of the return processing saga.
/// </summary>
public enum ReturnOutcome
{
    /// <summary>Full refund processed, inventory restocked.</summary>
    FullRefund,

    /// <summary>Partial refund processed (item damaged by customer).</summary>
    PartialRefund,

    /// <summary>Return label creation failed.</summary>
    LabelCreationFailed,

    /// <summary>Refund processing failed.</summary>
    RefundFailed,

    /// <summary>Saga is still in progress (awaiting package).</summary>
    InProgress
}
