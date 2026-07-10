using AotExample.Contracts.Events;
using Mocha.Sagas;

namespace AotExample.OrderService.Sagas;

public sealed class OrderSaga : Saga<OrderSagaState>
{
    private const string AwaitingShipment = nameof(AwaitingShipment);
    private const string Shipped = nameof(Shipped);

    protected override void Configure(ISagaDescriptor<OrderSagaState> descriptor)
    {
        descriptor.Timeout(TimeSpan.FromSeconds(30));

        descriptor
            .Initially()
            .OnEvent<OrderPlacedEvent>()
            .StateFactory(e => new OrderSagaState
            {
                OrderId = e.OrderId,
                ProductName = e.ProductName,
                Quantity = e.Quantity
            })
            .TransitionTo(AwaitingShipment);

        descriptor
            .During(AwaitingShipment)
            .OnEvent<OrderShippedEvent>()
            .Then((state, e) => state.TrackingNumber = e.TrackingNumber)
            .TransitionTo(Shipped);

        descriptor
            .Finally(Shipped);
    }
}
