namespace Demo.Shipping.Entities;

public class Shipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string Address { get; set; }
    public ShipmentStatus Status { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public ICollection<ShipmentItem> Items { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? EstimatedDelivery { get; set; }
}

public enum ShipmentStatus
{
    Pending,
    Processing,
    Shipped,
    InTransit,
    Delivered,
    Cancelled
}
