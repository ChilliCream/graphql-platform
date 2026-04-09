using Mocha;

namespace KafkaTransport.Contracts.Requests;

public sealed class GetOrderStatusRequest : IEventRequest<GetOrderStatusResponse>
{
    public required Guid OrderId { get; init; }
}
