using Mocha;
using AzureServiceBusTransport.Contracts.Events;

namespace AzureServiceBusTransport.OrderService.Handlers;

public sealed class OrderShippedEventHandler(ILogger<OrderShippedEventHandler> logger)
    : IEventHandler<OrderShippedEvent>
{
    public ValueTask HandleAsync(OrderShippedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} shipped via {Carrier}, tracking: {TrackingNumber}",
            message.OrderId,
            message.Carrier,
            message.TrackingNumber);

        return ValueTask.CompletedTask;
    }
}
