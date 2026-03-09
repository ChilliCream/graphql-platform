using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class RequestReplyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public RequestReplyTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnTypedResponse_When_HandlerRegistered()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.NotNull(response);
        Assert.Equal("ORD-1", response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    [Fact]
    public async Task RequestAsync_Should_CorrelateResponses_When_ConcurrentRequests()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        // act
        var tasks = new Task<OrderStatusResponse>[10];
        for (var i = 0; i < 10; i++)
        {
            using var scope = bus.Provider.CreateScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            tasks[i] = messageBus
                .RequestAsync(new GetOrderStatus { OrderId = $"ORD-{i}" }, CancellationToken.None)
                .AsTask();
        }

        var responses = await Task.WhenAll(tasks);

        // assert
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
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.RequestAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m },
            CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the request within timeout");
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnCorrectResponse_When_MultipleRequestTypesRegistered()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRequestHandler<GetShipmentStatusHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var orderResponse = await messageBus.RequestAsync(
            new GetOrderStatus { OrderId = "ORD-1" },
            CancellationToken.None);

        var shipmentResponse = await messageBus.RequestAsync(
            new GetShipmentStatus { TrackingNumber = "TRK-1" },
            CancellationToken.None);

        // assert
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
