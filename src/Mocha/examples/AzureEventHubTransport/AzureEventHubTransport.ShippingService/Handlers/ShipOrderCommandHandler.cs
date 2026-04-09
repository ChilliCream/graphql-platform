using Mocha;
using AzureEventHubTransport.Contracts.Commands;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.ShippingService.Handlers;

/// <summary>
/// Handles <see cref="ShipOrderCommand"/> sent by the fulfillment saga.
/// Simulates shipping, then publishes <see cref="OrderShippedEvent"/> which
/// the saga correlates to complete the workflow.
/// </summary>
public sealed class ShipOrderCommandHandler(
    IMessageBus messageBus,
    ILogger<ShipOrderCommandHandler> logger)
    : IEventHandler<ShipOrderCommand>
{
    private static readonly string[] Carriers = ["FedEx", "UPS", "DHL", "USPS"];

    public async ValueTask HandleAsync(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Preparing shipment for order {OrderId}: {Quantity}x {ProductName} → {CustomerEmail}",
            command.OrderId,
            command.Quantity,
            command.ProductName,
            command.CustomerEmail);

        // Simulate shipping processing time
        await Task.Delay(500, cancellationToken);

        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var carrier = Carriers[Random.Shared.Next(Carriers.Length)];

        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = command.OrderId,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                ShippedAt = DateTimeOffset.UtcNow,
                CorrelationId = command.CorrelationId
            },
            cancellationToken);

        logger.LogInformation(
            "Order {OrderId} shipped via {Carrier}, tracking: {TrackingNumber}",
            command.OrderId, carrier, trackingNumber);
    }
}
