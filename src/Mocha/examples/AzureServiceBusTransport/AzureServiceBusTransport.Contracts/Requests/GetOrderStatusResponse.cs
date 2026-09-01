namespace AzureServiceBusTransport.Contracts.Requests;

public sealed class GetOrderStatusResponse
{
    public required Guid OrderId { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
