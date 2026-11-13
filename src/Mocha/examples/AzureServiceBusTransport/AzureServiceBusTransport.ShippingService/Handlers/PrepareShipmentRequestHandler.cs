using AzureServiceBusTransport.Contracts.Commands;
using Mocha;

namespace AzureServiceBusTransport.ShippingService.Handlers;

/// <summary>
/// Handles PrepareShipmentCommand sent by the OrderFulfillmentSaga.
/// Assigns a carrier and tracking number, then responds so the saga can proceed.
/// </summary>
public sealed class PrepareShipmentRequestHandler(
    ILogger<PrepareShipmentRequestHandler> logger)
    : IEventRequestHandler<PrepareShipmentCommand, ShipmentPreparedResponse>
{
    private static readonly string[] s_carriers = ["FedEx", "UPS", "DHL", "USPS"];

    public async ValueTask<ShipmentPreparedResponse> HandleAsync(
        PrepareShipmentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Preparing shipment for order {OrderId}: {Quantity}x {ProductName}",
            request.OrderId, request.Quantity, request.ProductName);

        // Simulate preparation time
        await Task.Delay(300, cancellationToken);

        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var carrier = s_carriers[Random.Shared.Next(s_carriers.Length)];

        logger.LogInformation(
            "Shipment prepared for order {OrderId}: {Carrier} / {TrackingNumber}",
            request.OrderId, carrier, trackingNumber);

        return new ShipmentPreparedResponse
        {
            OrderId = request.OrderId,
            TrackingNumber = trackingNumber,
            Carrier = carrier,
            PreparedAt = DateTimeOffset.UtcNow
        };
    }
}
