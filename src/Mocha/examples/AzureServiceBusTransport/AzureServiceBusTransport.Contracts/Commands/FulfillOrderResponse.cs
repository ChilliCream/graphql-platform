namespace AzureServiceBusTransport.Contracts.Commands;

public sealed class FulfillOrderResponse
{
    public required Guid OrderId { get; init; }

    public required string Status { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset FulfilledAt { get; init; }
}
