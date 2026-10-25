using AotExample.OrderService.Commands;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class PlaceOrderCommandHandler(ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public ValueTask<PlaceOrderResult> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid().ToString("N")[..8];

        logger.LogOrderPlaced(orderId, command.Quantity, command.ProductName);

        return new ValueTask<PlaceOrderResult>(new PlaceOrderResult { OrderId = orderId });
    }
}

internal static partial class Logs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} placed: {Quantity}x {ProductName}")]
    public static partial void LogOrderPlaced(this ILogger logger, string orderId, int quantity, string productName);
}
