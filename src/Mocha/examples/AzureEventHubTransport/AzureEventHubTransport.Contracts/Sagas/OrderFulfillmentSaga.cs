using AzureEventHubTransport.Contracts.Commands;
using AzureEventHubTransport.Contracts.Events;
using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Sagas;

/// <summary>
/// Orchestrates the order fulfillment workflow:
///   1. OrderPlacedEvent kicks off the saga → sends ProcessPaymentCommand
///   2. PaymentProcessedEvent arrives → sends ShipOrderCommand
///   3. OrderShippedEvent arrives → publishes OrderFulfilledEvent, saga completes
/// </summary>
public sealed class OrderFulfillmentSaga : Saga<OrderFulfillmentState>
{
    public const string AwaitingPayment = nameof(AwaitingPayment);
    public const string AwaitingShipment = nameof(AwaitingShipment);
    public const string Completed = nameof(Completed);

    protected override void Configure(ISagaDescriptor<OrderFulfillmentState> saga)
    {
        saga.Initially()
            .OnEvent<OrderPlacedEvent>()
            .Then(static (state, _) => { })
            .StateFactory(evt => new OrderFulfillmentState
            {
                OrderId = evt.OrderId,
                ProductName = evt.ProductName,
                Quantity = evt.Quantity,
                TotalAmount = evt.TotalAmount,
                CustomerEmail = evt.CustomerEmail
            })
            .Send<ProcessPaymentCommand>(
                static (_, state) => new ProcessPaymentCommand
                {
                    OrderId = state.OrderId,
                    Amount = state.TotalAmount,
                    CustomerEmail = state.CustomerEmail,
                    CorrelationId = state.Id
                })
            .TransitionTo(AwaitingPayment);

        saga.During(AwaitingPayment)
            .OnEvent<PaymentProcessedEvent>()
            .Then(static (state, evt) => state.PaymentId = evt.PaymentId)
            .Send<ShipOrderCommand>(
                static (_, state) => new ShipOrderCommand
                {
                    OrderId = state.OrderId,
                    ProductName = state.ProductName,
                    Quantity = state.Quantity,
                    CustomerEmail = state.CustomerEmail,
                    CorrelationId = state.Id
                })
            .TransitionTo(AwaitingShipment);

        saga.During(AwaitingShipment)
            .OnEvent<OrderShippedEvent>()
            .Then(static (state, evt) =>
            {
                state.TrackingNumber = evt.TrackingNumber;
                state.Carrier = evt.Carrier;
            })
            .Publish<OrderFulfilledEvent>(
                static (_, state) => new OrderFulfilledEvent
                {
                    OrderId = state.OrderId,
                    PaymentId = state.PaymentId!.Value,
                    TrackingNumber = state.TrackingNumber!,
                    Carrier = state.Carrier!,
                    FulfilledAt = DateTimeOffset.UtcNow
                })
            .TransitionTo(Completed);

        saga.Finally(Completed);
    }
}
