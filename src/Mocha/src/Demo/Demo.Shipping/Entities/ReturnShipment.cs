namespace Demo.Shipping.Entities;

public class ReturnShipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OriginalShipmentId { get; set; }
    public Shipment? OriginalShipment { get; set; }
    public required string CustomerAddress { get; set; }
    public required string CustomerId { get; set; }
    public string? TrackingNumber { get; set; }
    public string? LabelUrl { get; set; }
    public ReturnShipmentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }

    // Order details for saga processing when package arrives
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public enum ReturnShipmentStatus
{
    LabelCreated,
    InTransit,
    Received,
    Cancelled
}
