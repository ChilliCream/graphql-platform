using KafkaTransport.Contracts.Events;
using Mocha.Sagas;

namespace KafkaTransport.OrderService.Sagas;

/// <summary>
/// Tracks an order from placement through shipping to fulfillment.
/// Reacts to events published by other services (choreography pattern).
/// </summary>
public sealed class OrderFulfillmentSaga : Saga<OrderFulfillmentState>
{
    private const string AwaitingShipment = nameof(AwaitingShipment);
    private const string Fulfilled = nameof(Fulfilled);

    protected override void Configure(ISagaDescriptor<OrderFulfillmentState> descriptor)
    {
        // Order placed → create saga state, wait for shipping
        descriptor
            .Initially()
            .OnEvent<OrderPlacedEvent>()
            .StateFactory(OrderFulfillmentState.FromOrderPlaced)
            .TransitionTo(AwaitingShipment);

        // Shipment confirmed → record tracking info, transition to fulfilled
        descriptor
            .During(AwaitingShipment)
            .OnEvent<OrderShippedEvent>()
            .Then((state, e) =>
            {
                state.TrackingNumber = e.TrackingNumber;
                state.Carrier = e.Carrier;
                state.ShippedAt = e.ShippedAt;
            })
            .TransitionTo(Fulfilled);

        // Order fulfilled → publish summary event, saga completes
        descriptor
            .Finally(Fulfilled)
            .OnEntry()
            .Publish<OrderFulfilledEvent>((_, state) => state.ToFulfilledEvent(), null);
    }
}
