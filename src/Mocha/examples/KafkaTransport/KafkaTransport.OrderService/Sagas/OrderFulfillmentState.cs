using KafkaTransport.Contracts.Events;
using Mocha.Sagas;

namespace KafkaTransport.OrderService.Sagas;

public sealed class OrderFulfillmentState : SagaStateBase
{
    public Guid OrderId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTimeOffset PlacedAt { get; set; }

    public string? TrackingNumber { get; set; }

    public string? Carrier { get; set; }

    public DateTimeOffset? ShippedAt { get; set; }

    public static OrderFulfillmentState FromOrderPlaced(OrderPlacedEvent e)
    {
        return new OrderFulfillmentState
        {
            Id = e.OrderId,
            OrderId = e.OrderId,
            ProductName = e.ProductName,
            Quantity = e.Quantity,
            TotalAmount = e.TotalAmount,
            CustomerEmail = e.CustomerEmail,
            PlacedAt = e.PlacedAt
        };
    }

    public OrderFulfilledEvent ToFulfilledEvent()
    {
        return new OrderFulfilledEvent
        {
            OrderId = OrderId,
            ProductName = ProductName,
            TrackingNumber = TrackingNumber ?? string.Empty,
            Carrier = Carrier ?? string.Empty,
            PlacedAt = PlacedAt,
            ShippedAt = ShippedAt ?? DateTimeOffset.UtcNow,
            FulfilledAt = DateTimeOffset.UtcNow
        };
    }
}
