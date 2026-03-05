using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.IntegrationTests;

public abstract class ConsumerIntegrationTestsBase
{
    protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    protected static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        return await CreateBusAsync(configure, configureTransport: null);
    }

    protected static async Task<ServiceProvider> CreateBusAsync(
        Action<IMessageBusHostBuilder> configure,
        Action<IInMemoryMessagingTransportDescriptor>? configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);

        if (configureTransport is not null)
        {
            builder.AddInMemory(configureTransport);
        }
        else
        {
            builder.AddInMemory();
        }

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }
}

public sealed class OrderCreatedKeyedHandler1([FromKeyedServices("r1")] MessageRecorder recorder)
    : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}

public sealed class OrderCreatedKeyedHandler2([FromKeyedServices("r2")] MessageRecorder recorder)
    : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}

public sealed class OrderCreated
{
    public required string OrderId { get; init; }
}

public sealed class ItemShipped
{
    public required string TrackingNumber { get; init; }
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

public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
    {
        recorder.Record(request);
        return default;
    }
}

public sealed class GetOrderStatusHandler(MessageRecorder recorder)
    : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
    {
        recorder.Record(request);
        return new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
    }
}

public sealed class NullResponseHandler(MessageRecorder recorder)
    : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
    {
        recorder.Record(request);
        return new(default(OrderStatusResponse)!);
    }
}

public sealed class ThrowingEventHandler([FromKeyedServices("throwing")] MessageRecorder recorder)
    : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        throw new InvalidOperationException("Handler failed deliberately");
    }
}

public sealed class ThrowingRequestHandler(MessageRecorder recorder)
    : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
    {
        recorder.Record(request);
        throw new InvalidOperationException("Request handler failed deliberately");
    }
}

public sealed class DependencyHandler(InvocationCounter counter, MessageRecorder recorder) : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        counter.Increment();
        recorder.Record(message);
        return default;
    }
}

public sealed class OrderCreatedKeyedHandler([FromKeyedServices("order")] MessageRecorder recorder)
    : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}

public sealed class ItemShippedKeyedHandler([FromKeyedServices("shipment")] MessageRecorder recorder)
    : IEventHandler<ItemShipped>
{
    public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}

public sealed class GetOrderStatusKeyedHandler([FromKeyedServices("request")] MessageRecorder recorder)
    : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
    {
        recorder.Record(request);
        return new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
    }
}
