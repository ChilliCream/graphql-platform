using Mocha;

namespace PostgresTransport.Contracts.Requests;

public sealed class GetOrderStatusRequest : IEventRequest<GetOrderStatusResponse>
{
    public required Guid OrderId { get; init; }
}
