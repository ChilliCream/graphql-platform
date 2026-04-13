namespace Demo.Shipping.Entities;

public class ShipmentItem
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
}
