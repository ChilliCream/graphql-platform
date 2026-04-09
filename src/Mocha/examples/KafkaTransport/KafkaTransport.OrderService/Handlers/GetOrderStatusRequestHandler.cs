using Mocha;
using KafkaTransport.Contracts.Requests;

namespace KafkaTransport.OrderService.Handlers;

public sealed class GetOrderStatusRequestHandler
    : IEventRequestHandler<GetOrderStatusRequest, GetOrderStatusResponse>
{
    public ValueTask<GetOrderStatusResponse> HandleAsync(
        GetOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        // In a real app this would query a database
        return new(new GetOrderStatusResponse
        {
            OrderId = request.OrderId,
            Status = "Processing",
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
