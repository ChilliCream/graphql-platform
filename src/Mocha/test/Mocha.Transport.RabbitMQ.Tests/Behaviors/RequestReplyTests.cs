using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Messaging)]
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

    [Fact]
    public async Task Transport_Should_DeclareDurableExpiringReplyQueue_When_Started()
    {
        // arrange
        // The broker denies non-durable, non-exclusive queues by default since RabbitMQ 4.3, so a
        // host that starts at all proves the reply queue shape is accepted. The listing pins it.
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        // act
        var output = await _fixture.InvokeCommandAsync(
            [
                "rabbitmqctl",
                "list_queues",
                "name",
                "durable",
                "auto_delete",
                "exclusive",
                "arguments",
                "-p",
                vhost.VhostName,
                "--quiet",
                "--formatter",
                "json"
            ]);
        var replyQueue = JsonDocument.Parse(output).RootElement
            .EnumerateArray()
            .Single(queue => queue.GetProperty("name").GetString()!.StartsWith("response-", StringComparison.Ordinal));

        // each argument is listed as [name, type, value], and RabbitMQ 4 also lists the queue type it applied
        var expires = replyQueue.GetProperty("arguments")
            .EnumerateArray()
            .Single(argument => argument[0].GetString() == "x-expires")[2]
            .GetInt32();

        // assert
        Assert.True(replyQueue.GetProperty("durable").GetBoolean());
        Assert.False(replyQueue.GetProperty("exclusive").GetBoolean());
        Assert.False(replyQueue.GetProperty("auto_delete").GetBoolean());
        Assert.Equal(60000, expires);
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnResponse_When_ConsumerConnectionIsLost()
    {
        // arrange
        const string queueName = "order-status";
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t => t.Endpoint(queueName).Handler<GetOrderStatusHandler>())
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
        var before = await messageBus.RequestAsync(new GetOrderStatus { OrderId = "ORD-before" }, cts.Token);
        var replyQueue = await vhost.GetReplyQueueNameAsync();

        // act
        // the reply queue expires while its consumer is disconnected
        await using (await vhost.RevokeAccessAsync())
        {
            await vhost.CloseConnectionOfConsumerAsync(queueName);
            await vhost.DeleteQueueAsync(replyQueue);
        }

        var after = await messageBus.RequestAsync(new GetOrderStatus { OrderId = "ORD-after" }, cts.Token);

        // assert
        Assert.Equal(("ORD-before", "Shipped"), (before.OrderId, before.Status));
        Assert.Equal(("ORD-after", "Shipped"), (after.OrderId, after.Status));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestAsync_Should_ReturnAllResponses_When_RequestsWaitForReplyRecovery(
        bool automaticRecovery)
    {
        // arrange
        const string queueName = "order-status";
        var timeout = TimeSpan.FromSeconds(60);
        await using var vhost = await _fixture.CreateVhostAsync();
        var connectionFactory = (ConnectionFactory)vhost.ConnectionFactory;
        connectionFactory.AutomaticRecoveryEnabled = automaticRecovery;
        connectionFactory.NetworkRecoveryInterval = TimeSpan.FromSeconds(2);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t => t.Endpoint(queueName).Handler<GetOrderStatusHandler>())
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout);
        await messageBus.RequestAsync(new GetOrderStatus { OrderId = "ORD-before" }, cts.Token);
        var replyQueue = await vhost.GetReplyQueueNameAsync();
        await vhost.SetConfigurePermissionAsync("^$");
        Task<OrderStatusResponse>[] pendingRequests;

        // act
        try
        {
            // the reply queue expires while its consumer is disconnected
            await vhost.CloseConnectionOfConsumerAsync(queueName);
            await vhost.DeleteQueueAsync(replyQueue);
            pendingRequests = Enumerable.Range(1, 3)
                .Select(i => messageBus.RequestAsync(new GetOrderStatus { OrderId = $"ORD-{i}" }, cts.Token).AsTask())
                .ToArray();
            Assert.Equal(0, await vhost.GetMessageCountAsync(queueName));
            Assert.All(pendingRequests, request => Assert.False(request.IsCompleted));
        }
        finally
        {
            await vhost.SetConfigurePermissionAsync(".*");
        }

        var responses = await Task.WhenAll(pendingRequests).WaitAsync(cts.Token);

        // assert
        Assert.Equal(
            [("ORD-1", "Shipped"), ("ORD-2", "Shipped"), ("ORD-3", "Shipped")],
            responses.Select(r => (r.OrderId, r.Status)));
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
