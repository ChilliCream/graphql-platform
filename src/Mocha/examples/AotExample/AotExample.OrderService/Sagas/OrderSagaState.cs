using Mocha.Sagas;

namespace AotExample.OrderService.Sagas;

public class OrderSagaState : SagaStateBase
{
    public string OrderId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? TrackingNumber { get; set; }
}
