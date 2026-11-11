using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConsumerAttachment)]
public sealed class RequestReadinessTests(RabbitMQFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestAsync_Should_WaitForReplyConsumer_When_ReplyQueueRecoversOnSeparateBus(bool cancel)
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
        await using var user = await vhost.Faults.AddUserAsync(requesterFactory.UserName, requesterFactory.Password);
        await using var requester = await new ServiceCollection()
            .AddSingleton<IConnectionFactory>(requesterFactory)
            .AddMessageBus()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var replyQueue = ((RabbitMQReceiveEndpoint)requester.Transport.ReplyReceiveEndpoint!).Queue.Name;
        var handled = new MessageRecorder();
        var replyConsumers = new ReplyConsumerProbe(vhost.Broker, replyQueue);
        await using var responder = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(handled)
            .AddSingleton(replyConsumers)
            .AddMessageBus()
            .AddRequestHandler<StatusHandler>()
            .AddRabbitMQ(t => t.Endpoint("order-status").Handler<StatusHandler>())
            .BuildTestBusAsync();
        using var scope = requester.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new SendOptions { Endpoint = new Uri("rabbitmq:q/order-status") };
        await bus.RequestAsync(new GetOrderStatus { OrderId = "before" }, options, TestContext.Current.CancellationToken);
        var replyConsumer = await vhost.Broker.GetConsumerAsync(replyQueue);
        var consumerConnection = await requester.Transport.ConsumerManager.Connection.GetConnectionAsync(
            TestContext.Current.CancellationToken);
        var consumerConnectionLost = consumerConnection.WhenShutdownAsync();

        // the requester cannot declare its reply queue again, so its reply consumer stays unavailable
        await vhost.Faults.SetPermissionsAsync(requesterFactory.UserName, configure: "^$", write: ".*", read: ".*");
        await vhost.Faults.CloseConsumerConnectionAsync(replyQueue);
        await consumerConnectionLost.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        await vhost.Faults.DeleteQueueAsync(replyQueue);

        // act
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TestTimeouts.Signal);
        var pending = bus.RequestAsync(new GetOrderStatus { OrderId = "after" }, options, deadline.Token).AsTask();
        try
        {
            var requestQueueWhileUnavailable = await vhost.Broker.GetQueueAsync("order-status");
            var handledWhileUnavailable = handled.Messages.Cast<GetOrderStatus>().Select(m => m.OrderId).ToArray();
            if (cancel)
            {
                await deadline.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            }

            await vhost.Faults.SetPermissionsAsync(requesterFactory.UserName, configure: ".*", write: ".*", read: ".*");
            await vhost.Broker.WaitForConsumerAsync(replyQueue, otherThan: replyConsumer);
            var reply = cancel
                ? null
                : await pending.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

            await bus.RequestAsync(new GetOrderStatus { OrderId = "probe" }, options, deadline.Token.IsCancellationRequested
                ? TestContext.Current.CancellationToken
                : deadline.Token);

            // assert
            Assert.Equal(cancel ? null : "after", reply?.OrderId);
            Assert.Equal(new QueueInfo(Consumers: 1, Ready: 0, Unacknowledged: 0), requestQueueWhileUnavailable);
            Assert.Equal(["before"], handledWhileUnavailable);

            // every request was handled while its reply queue had a consumer, so "after" was held back until then
            Assert.Equal(
                cancel
                    ? [("before", 1), ("probe", 1)]
                    : new (string, int?)[] { ("after", 1), ("before", 1), ("probe", 1) },
                replyConsumers.AtHandleTime.OrderBy(request => request.OrderId));
        }
        finally
        {
            await vhost.Faults.SetPermissionsAsync(requesterFactory.UserName, configure: ".*", write: ".*", read: ".*");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_Deliver_When_LocalReplyConsumerIsUnavailable(bool customReplyAddress)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var sender = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t => t.DeclareQueue("outbound"))
            .BuildTestBusAsync();
        var transport = sender.Transport;
        var replyQueue = ((RabbitMQReceiveEndpoint)transport.ReplyReceiveEndpoint!).Queue.Name;
        using var scope = sender.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new SendOptions
        {
            Endpoint = new Uri("rabbitmq:q/outbound"),
            ReplyEndpoint = customReplyAddress ? new Uri("rabbitmq://remote/q/replies") : null
        };
        await bus.SendAsync(new OrderCreated { OrderId = "before" }, options, TestContext.Current.CancellationToken);
        await using var declaresDenied = await vhost.Faults.DenyDeclaresAsync();
        await vhost.Faults.CloseConsumerConnectionAsync(replyQueue);
        await vhost.Faults.DeleteQueueAsync(replyQueue);

        // act
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TestTimeouts.Signal);
        await bus.SendAsync(new OrderCreated { OrderId = "after" }, options, deadline.Token);
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(deadline.Token);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: deadline.Token);
        var first = await channel.BasicGetAsync("outbound", true, deadline.Token);
        var second = await channel.BasicGetAsync("outbound", true, deadline.Token);
        var remaining = await channel.MessageCountAsync("outbound", deadline.Token);

        // assert
        Assert.False(transport.ConsumerManager.IsHealthy);
        Assert.Equal(options.ReplyEndpoint?.ToString(), first!.BasicProperties.ReplyTo);
        Assert.Equal(options.ReplyEndpoint?.ToString(), second!.BasicProperties.ReplyTo);
        Assert.Equal(0u, remaining);
    }

    /// <summary>
    /// Records how many consumers the reply queue of the requester has at the moment a request is handled.
    /// </summary>
    public sealed class ReplyConsumerProbe(BrokerView broker, string replyQueue)
    {
        private readonly ConcurrentQueue<(string OrderId, int? ReplyConsumers)> _atHandleTime = new();

        public IEnumerable<(string OrderId, int? ReplyConsumers)> AtHandleTime => _atHandleTime;

        public async Task ObserveAsync(string orderId)
            => _atHandleTime.Enqueue((orderId, (await broker.GetQueueAsync(replyQueue))?.Consumers));
    }

    public sealed class StatusHandler(MessageRecorder recorder, ReplyConsumerProbe replyConsumers)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public async ValueTask<OrderStatusResponse> HandleAsync(
            GetOrderStatus request,
            CancellationToken cancellationToken)
        {
            recorder.Record(request);
            await replyConsumers.ObserveAsync(request.OrderId);
            return new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" };
        }
    }
}
