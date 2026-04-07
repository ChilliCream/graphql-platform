using AotExample.OrderService.Commands;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed partial class PlaceOrderCommandHandler(
    ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid().ToString("N")[..8];

        LogOrderPlaced(orderId, command.Quantity, command.ProductName);

        return new ValueTask<PlaceOrderResult>(
            new PlaceOrderResult { OrderId = orderId });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} placed: {Quantity}x {ProductName}")]
    private partial void LogOrderPlaced(string orderId, int quantity, string productName);
}
