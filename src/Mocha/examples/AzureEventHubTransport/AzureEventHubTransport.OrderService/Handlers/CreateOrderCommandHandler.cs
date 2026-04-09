using Mocha.Mediator;
using AzureEventHubTransport.Contracts.Mediator;

namespace AzureEventHubTransport.OrderService.Handlers;

/// <summary>
/// Mediator handler — validates the order and returns the result.
/// Runs in-process before anything is published to the bus.
/// </summary>
public sealed class CreateOrderCommandHandler(ILogger<CreateOrderCommandHandler> logger)
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public ValueTask<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var totalAmount = command.UnitPrice * command.Quantity;

        logger.LogInformation(
            "Order {OrderId} created: {Quantity}x {Product} = ${Total} for {Email}",
            orderId, command.Quantity, command.ProductName, totalAmount, command.CustomerEmail);

        return new(new CreateOrderResult
        {
            OrderId = orderId,
            TotalAmount = totalAmount
        });
    }
}
