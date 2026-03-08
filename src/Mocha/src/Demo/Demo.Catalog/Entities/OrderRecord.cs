namespace Demo.Catalog.Entities;

public class OrderRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public required string CustomerId { get; set; }
    public required string ShippingAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum OrderStatus
{
    Pending,
    Paid,
    Shipping,
    Delivered,
    Cancelled,
    ReturnInitiated,
    Returned
}
