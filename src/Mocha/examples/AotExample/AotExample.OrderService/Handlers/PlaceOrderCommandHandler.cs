using AotExample.OrderService.Commands;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class PlaceOrderCommandHandler(
    ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid().ToString("N")[..8];

        logger.LogInformation(
            "Order {OrderId} placed: {Quantity}x {ProductName}",
            orderId,
            command.Quantity,
            command.ProductName);

        return new ValueTask<PlaceOrderResult>(
            new PlaceOrderResult { OrderId = orderId });
    }
}
