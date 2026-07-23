using Mocha.Sagas;

namespace AzureServiceBusTransport.OrderService.Sagas;

public sealed class OrderFulfillmentState(Guid id, string state) : SagaStateBase(id, state)
{
    public Guid OrderId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string? TrackingNumber { get; set; }

    public string? Carrier { get; set; }
}
