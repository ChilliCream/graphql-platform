using Mocha;

namespace AotExample.Contracts.Requests;

public sealed class CheckInventoryRequest : IEventRequest<CheckInventoryResponse>
{
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
}
