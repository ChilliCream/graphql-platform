using KafkaTransport.Contracts.Events;
using Mocha;
using Mocha.Mediator;

namespace KafkaTransport.OrderService.Mediator.Commands;

public sealed class PlaceOrderCommandHandler(IMessageBus messageBus)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();

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
