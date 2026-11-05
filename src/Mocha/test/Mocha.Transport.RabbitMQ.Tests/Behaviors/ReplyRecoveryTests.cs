using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class ReplyRecoveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RequestAsync_Should_FailWithinTimeout_When_BrokerStopped()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        await AssertRoundTripAsync(bus, recorder, "ORD-1");

        // act
        await using (await fixture.StopBrokerAsync())
        {
            for (var i = 0; i < 3; i++)
            {
                using var scope = bus.Provider.CreateScope();
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(2));
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => messageBus.RequestAsync(new GetOrderStatus { OrderId = $"ORD-{i + 2}" }, deadline.Token)
                        .AsTask().WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken));
            }
        }

        // assert
        await AssertRoundTripAsync(bus, recorder, "ORD-recovered");
    }

    [Fact]
    public async Task RequestAsync_Should_ReceiveReply_When_RequesterConsumerConnectionDropsBeforeReply()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var requesterFactory = new ConnectionFactory
        {
            Uri = vhost.ConnectionSettings.Uri,
            VirtualHost = vhost.VhostName,
            UserName = $"{vhost.VhostName}-requester",
            Password = "password"
        };
        await using var user = await vhost.AddUserAsync(requesterFactory.UserName, requesterFactory.Password);
        var gate = new ReplyGate();
        await using var responder = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddRequestHandler<GatedOrderStatusHandler>()
            .AddRabbitMQ(t => t.Endpoint("order-status").Handler<GatedOrderStatusHandler>())
            .BuildTestBusAsync();
        await using var requester = await new ServiceCollection()
            .AddSingleton<IConnectionFactory>(requesterFactory)
            .AddMessageBus()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var replyQueue = ((RabbitMQReceiveEndpoint)GetTransport(requester).ReplyReceiveEndpoint!).Queue.Name;

        // act
        using var scope = requester.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        var request = scope.ServiceProvider.GetRequiredService<IMessageBus>().RequestAsync(
            new GetOrderStatus { OrderId = "ORD-1" },
            new SendOptions { Endpoint = new Uri("rabbitmq:q/order-status") },
            cts.Token).AsTask();
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // the requester cannot attach again until its permissions are restored, so the reply is sent while its
        // reply queue has no consumer
        await vhost.SetPermissionsAsync(requesterFactory.UserName, configure: "^$", write: "^$", read: "^$");
        await vhost.CloseConnectionOfConsumerAsync(replyQueue);
        await vhost.WaitForNoConsumerAsync(replyQueue, s_timeout);
        gate.Release.TrySetResult();
        await vhost.WaitForMessagesAsync(replyQueue, 1, s_timeout);
        await vhost.SetPermissionsAsync(requesterFactory.UserName, configure: ".*", write: ".*", read: ".*");

        var reply = await request;

        // assert
        Assert.Equal("ORD-1", reply.OrderId);
        Assert.Equal("Shipped", reply.Status);
    }

    private static Task<TestBus> CreateBusAsync(VhostContext vhost, MessageRecorder recorder)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("orders").Handler<OrderCreatedHandler>();
                t.Endpoint("order-status").Handler<GetOrderStatusHandler>();
            })
            .BuildTestBusAsync();

    private static RabbitMQMessagingTransport GetTransport(TestBus bus)
        => bus.Provider.GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>().Single();

    private static async Task AssertRoundTripAsync(TestBus bus, MessageRecorder recorder, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CreateTimeout();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = orderId }, deadline.Token);
        Assert.True(await Condition.WaitAsync(
            () => recorder.Messages.Cast<OrderCreated>().Any(order => order.OrderId == orderId), s_timeout));
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = orderId }, deadline.Token);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    private static CancellationTokenSource CreateTimeout()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
        return cts;
    }

    public sealed class ReplyGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class GatedOrderStatusHandler(ReplyGate gate)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public async ValueTask<OrderStatusResponse> HandleAsync(
            GetOrderStatus request,
            CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            return new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" };
        }
    }
}
