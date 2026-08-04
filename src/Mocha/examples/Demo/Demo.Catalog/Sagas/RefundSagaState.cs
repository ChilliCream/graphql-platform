using Demo.Contracts.Commands;
using Demo.Contracts.Events;
using Demo.Contracts.Saga;
using Mocha.Sagas;

namespace Demo.Catalog.Sagas;

/// <summary>
/// Shared state for refund-related sagas.
/// </summary>
public class RefundSagaState : SagaStateBase
{
    // Order information
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string Reason { get; init; }

    // For return processing saga
    public Guid? ProductId { get; init; }
    public int Quantity { get; init; }
    public string? CustomerAddress { get; init; }
    public Guid? OriginalShipmentId { get; init; }

    // Results from steps
    public Guid? ReturnId { get; set; }
    public string? ReturnTrackingNumber { get; set; }
    public Guid? RefundId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public decimal? RefundPercentage { get; set; }
    public bool InventoryRestocked { get; set; }
    public int? QuantityRestocked { get; set; }
    public InspectionResult? InspectionResult { get; set; }

    // Failure tracking
    public string? FailureReason { get; set; }
    public string? FailureStage { get; set; }

    /// <summary>
    /// Create state from quick refund request.
    /// </summary>
    public static RefundSagaState FromQuickRefund(RequestQuickRefundRequest request)
        => new()
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            CustomerId = request.CustomerId,
            Reason = request.Reason
        };

    /// <summary>
    /// Create state from return package received event.
    /// </summary>
    public static RefundSagaState FromReturnPackageReceived(ReturnPackageReceivedEvent evt)
        => new()
        {
            OrderId = evt.OrderId,
            Amount = evt.Amount,
            CustomerId = evt.CustomerId,
            Reason = evt.Reason ?? "Return requested",
            ProductId = evt.ProductId,
            Quantity = evt.Quantity,
            ReturnId = evt.ReturnId,
            ReturnTrackingNumber = evt.TrackingNumber
        };

    /// <summary>
    /// Create refund command for billing.
    /// </summary>
    public ProcessRefundCommand ToProcessRefund()
        => new()
        {
            OrderId = OrderId,
            Amount = Amount,
            Reason = Reason,
            CustomerId = CustomerId
        };

    /// <summary>
    /// Create return label command for shipping.
    /// Note: This is no longer used by the saga - labels are created directly by the API.
    /// </summary>
    public CreateReturnLabelCommand ToCreateReturnLabel()
        => new()
        {
            OrderId = OrderId,
            OriginalShipmentId = OriginalShipmentId!.Value,
            CustomerAddress = CustomerAddress!,
            CustomerId = CustomerId,
            ProductId = ProductId!.Value,
            Quantity = Quantity,
            Amount = Amount,
            Reason = Reason
        };

    /// <summary>
    /// Create inspect return command for catalog.
    /// </summary>
    public InspectReturnCommand ToInspectReturn()
        => new()
        {
            OrderId = OrderId,
            ProductId = ProductId!.Value,
            Quantity = Quantity,
            ReturnId = ReturnId!.Value
        };

    /// <summary>
    /// Create restock command for catalog.
    /// </summary>
    public RestockInventoryCommand ToRestockInventory()
        => new()
        {
            OrderId = OrderId,
            ProductId = ProductId!.Value,
            Quantity = Quantity,
            ReturnId = ReturnId!.Value
        };

    /// <summary>
    /// Create partial refund command for billing.
    /// </summary>
    public ProcessPartialRefundCommand ToProcessPartialRefund(decimal percentage)
        => new()
        {
            OrderId = OrderId,
            OriginalAmount = Amount,
            RefundPercentage = percentage,
            Reason = $"{Reason} - Item damaged by customer",
            CustomerId = CustomerId
        };
}
