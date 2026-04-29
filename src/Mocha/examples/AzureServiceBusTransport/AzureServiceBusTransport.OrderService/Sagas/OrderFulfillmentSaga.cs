using AzureServiceBusTransport.Contracts.Commands;
using AzureServiceBusTransport.Contracts.Events;
using Mocha.Sagas;

namespace AzureServiceBusTransport.OrderService.Sagas;

/// <summary>
/// Orchestrates the end-to-end fulfillment of an order.
///
/// Flow:
///   1. Receives a FulfillOrderRequest (request-reply)
///   2. Sends PrepareShipmentCommand to the shipping service and waits
///   3. On ShipmentPreparedResponse, publishes OrderShippedEvent for
///      downstream subscribers (notifications, analytics)
///   4. Responds with FulfillOrderResponse to the original caller
/// </summary>
public sealed class OrderFulfillmentSaga : Saga<OrderFulfillmentState>
{
    protected override void Configure(ISagaDescriptor<OrderFulfillmentState> saga)
    {
        saga.Initially()
            .OnRequest<FulfillOrderRequest>()
            .StateFactory(req => new OrderFulfillmentState(Guid.NewGuid(), "__Initial")
            {
                OrderId = req.OrderId,
                ProductName = req.ProductName,
                Quantity = req.Quantity,
                TotalAmount = req.TotalAmount,
                CustomerEmail = req.CustomerEmail
            })
            .Send(state => new PrepareShipmentCommand
            {
                OrderId = state.OrderId,
                ProductName = state.ProductName,
                Quantity = state.Quantity
            })
            .TransitionTo("AwaitingShipment");

        saga.During("AwaitingShipment")
            .OnReply<ShipmentPreparedResponse>()
            .Then((state, reply) =>
            {
                state.TrackingNumber = reply.TrackingNumber;
                state.Carrier = reply.Carrier;
            })
            .Publish(state => new OrderShippedEvent
            {
                OrderId = state.OrderId,
                TrackingNumber = state.TrackingNumber!,
                Carrier = state.Carrier!,
                ShippedAt = DateTimeOffset.UtcNow
            })
            .TransitionTo("Fulfilled");

        saga.Finally("Fulfilled")
            .Respond(state => new FulfillOrderResponse
            {
                OrderId = state.OrderId,
                Status = "Fulfilled",
                TrackingNumber = state.TrackingNumber!,
                Carrier = state.Carrier!,
                FulfilledAt = DateTimeOffset.UtcNow
            });
    }
}
