namespace AotExample.Contracts.Queries;

public sealed class GetOrderStatusResponse
{
    public required string OrderId { get; init; }

    public required string Status { get; init; }
}
