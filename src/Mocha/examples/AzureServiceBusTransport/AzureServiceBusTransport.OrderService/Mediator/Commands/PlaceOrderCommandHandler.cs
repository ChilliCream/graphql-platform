using AzureServiceBusTransport.Contracts.Events;
using Mocha;
using Mocha.Mediator;

namespace AzureServiceBusTransport.OrderService.Mediator.Commands;

/// <summary>
/// Handles the in-process PlaceOrderCommand by publishing an OrderPlacedEvent
/// to the message bus for cross-service fan-out.
/// </summary>
public sealed class PlaceOrderCommandHandler(
    IMessageBus messageBus,
    ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();

        logger.LogInformation(
            "Placing order {OrderId}: {Quantity}x {ProductName} for {CustomerEmail}",
            orderId, command.Quantity, command.ProductName, command.CustomerEmail);

        await messageBus.PublishAsync(
            new OrderPlacedEvent
            {
                OrderId = orderId,
                ProductName = command.ProductName,
                Quantity = command.Quantity,
                TotalAmount = command.UnitPrice * command.Quantity,
                CustomerEmail = command.CustomerEmail,
                PlacedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);

        return new PlaceOrderResult { OrderId = orderId, Status = "Placed" };
    }
}
