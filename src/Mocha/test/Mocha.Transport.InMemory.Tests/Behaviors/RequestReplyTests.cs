using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class RequestReplyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RequestAsync_Should_ReturnTypedResponse_When_HandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.NotNull(response);
        Assert.Equal("ORD-1", response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    [Fact]
    public async Task RequestAsync_Should_ThrowRemoteError_When_HandlerReturnsNull()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<NullResponseHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert — null response triggers an internal exception caught by the fault
        // middleware, which sends NotAcknowledgedEvent back as RemoteErrorException
        using var cts = new CancellationTokenSource(s_timeout);
        await Assert.ThrowsAsync<RemoteErrorException>(async () =>
            await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, cts.Token)
        );
    }

    [Fact]
    public async Task RequestAsync_Should_CorrelateResponses_When_ConcurrentRequests()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        // act - fire 10 concurrent requests
        var tasks = new Task<OrderStatusResponse>[10];
        for (var i = 0; i < 10; i++)
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            tasks[i] = bus.RequestAsync(new GetOrderStatus { OrderId = $"ORD-{i}" }, CancellationToken.None).AsTask();
        }

        var responses = await Task.WhenAll(tasks);

        // assert - each response matches its request
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal($"ORD-{i}", responses[i].OrderId);
            Assert.Equal("Shipped", responses[i].Status);
        }
    }

    [Fact]
    public async Task RequestAsync_Should_Complete_When_VoidRequestAcknowledged()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - RequestAsync for IEventRequest (no typed response) awaits AcknowledgedEvent
        await bus.RequestAsync(new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m }, CancellationToken.None);

        // assert - if we got here without exception, the acknowledgement round-trip succeeded
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the request within timeout");
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnCorrectResponse_When_MultipleRequestTypesRegistered()
    {
        // arrange - register two different request/response handlers
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<TestHelpers.GetOrderStatusHandler>()
            .AddRequestHandler<GetShipmentStatusHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - request both in sequence
        var orderResponse = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);

        var shipmentResponse = await bus.RequestAsync(
            new GetShipmentStatus { TrackingNumber = "TRK-1" },
            CancellationToken.None);

        // assert - each response type is correct and contains the right data
        Assert.Equal("ORD-1", orderResponse.OrderId);
        Assert.Equal("Shipped", orderResponse.Status);
        Assert.Equal("TRK-1", shipmentResponse.TrackingNumber);
        Assert.Equal("InTransit", shipmentResponse.Status);
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnResponse_When_HandlerUsesRequestDataInResponse()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<TestHelpers.GetOrderStatusHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send multiple requests with different IDs
        var response1 = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-AAA" }, CancellationToken.None);
        var response2 = await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-BBB" }, CancellationToken.None);

        // assert - responses carry the correct request-specific data
        Assert.Equal("ORD-AAA", response1.OrderId);
        Assert.Equal("ORD-BBB", response2.OrderId);
    }

    [Fact]
    public async Task RequestAsync_Should_CorrelateResponses_When_DifferentRequestTypesSentConcurrently()
    {
        // arrange
        await using var provider = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<TestHelpers.GetOrderStatusHandler>()
            .AddRequestHandler<GetShipmentStatusHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        // act - fire concurrent requests of different types
        var orderTask = Task.Run(async () =>
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            return await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);
        });

        var shipmentTask = Task.Run(async () =>
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            return await bus.RequestAsync(new GetShipmentStatus { TrackingNumber = "TRK-1" }, CancellationToken.None);
        });

        var orderResponse = await orderTask;
        var shipmentResponse = await shipmentTask;

        // assert - each response matches its request type
        Assert.Equal("ORD-1", orderResponse.OrderId);
        Assert.Equal("Shipped", orderResponse.Status);
        Assert.Equal("TRK-1", shipmentResponse.TrackingNumber);
        Assert.Equal("InTransit", shipmentResponse.Status);
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

    public sealed class GetShipmentStatus : IEventRequest<ShipmentStatusResponse>
    {
        public required string TrackingNumber { get; init; }
    }

    public sealed class ShipmentStatusResponse
    {
        public required string TrackingNumber { get; init; }
        public required string Status { get; init; }
    }

    public sealed class GetShipmentStatusHandler : IEventRequestHandler<GetShipmentStatus, ShipmentStatusResponse>
    {
        public ValueTask<ShipmentStatusResponse> HandleAsync(
            GetShipmentStatus request,
            CancellationToken cancellationToken)
        {
            return new(new ShipmentStatusResponse { TrackingNumber = request.TrackingNumber, Status = "InTransit" });
        }
    }
}
