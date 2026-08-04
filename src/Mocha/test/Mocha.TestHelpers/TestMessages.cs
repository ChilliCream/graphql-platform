namespace Mocha.TestHelpers;

public sealed class OrderCreated
{
    public required string OrderId { get; init; }
}

public sealed class ProcessPayment
{
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
}

public sealed class GetOrderStatus : IEventRequest<OrderStatusResponse>
{
    public required string OrderId { get; init; }
}

public sealed class OrderStatusResponse
{
    public required string OrderId { get; init; }
    public required string Status { get; init; }
}

public sealed class OrderCreatedHandler(MessageRecorder recorder) : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}

public sealed class GetOrderStatusHandler : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
    {
        return new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
    }
}
