using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public sealed class RequestReadinessTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

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
        await using var user = await vhost.AddUserAsync(requesterFactory.UserName, requesterFactory.Password);
        var handled = new MessageRecorder();
        await using var responder = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(handled)
            .AddMessageBus()
            .AddRequestHandler<StatusHandler>()
            .AddRabbitMQ(t => t.Endpoint("order-status").Handler<StatusHandler>())
            .BuildTestBusAsync();
        await using var requester = await new ServiceCollection()
            .AddSingleton<IConnectionFactory>(requesterFactory)
            .AddMessageBus()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var transport = requester.Provider.GetRequiredService<IMessagingRuntime>().Transports.OfType<RabbitMQMessagingTransport>().Single();
        var replyQueue = ((RabbitMQReceiveEndpoint)transport.ReplyReceiveEndpoint!).Queue.Name;
        using var scope = requester.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new SendOptions { Endpoint = new Uri("rabbitmq:q/order-status") };
        await bus.RequestAsync(new GetOrderStatus { OrderId = "before" }, options, TestContext.Current.CancellationToken);
        await vhost.SetPermissionsAsync(requesterFactory.UserName, configure: "^$", write: ".*", read: ".*");
        await vhost.CloseConnectionOfConsumerAsync(replyQueue);
        await vhost.DeleteQueueAsync(replyQueue);

        // act
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(s_timeout);
        var pending = bus.RequestAsync(new GetOrderStatus { OrderId = "after" }, options, deadline.Token).AsTask();
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
            var dispatchedWhileUnavailable = handled.Messages.Cast<GetOrderStatus>().Select(m => m.OrderId).ToArray();
            var completedWhileUnavailable = pending.IsCompleted;
            if (cancel)
            {
                await deadline.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            }

            await vhost.SetPermissionsAsync(requesterFactory.UserName, configure: ".*", write: ".*", read: ".*");
            await vhost.WaitForConsumerAsync(replyQueue, s_timeout);
            if (!cancel)
            {
                var reply = await pending.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
                Assert.Equal("after", reply.OrderId);
            }

            await bus.RequestAsync(new GetOrderStatus { OrderId = "probe" }, options, deadline.Token.IsCancellationRequested
                ? TestContext.Current.CancellationToken
                : deadline.Token);

            // assert
            Assert.Equal(["before"], dispatchedWhileUnavailable);
            Assert.False(completedWhileUnavailable);
            Assert.Equal(cancel ? ["before", "probe"] : new[] { "after", "before", "probe" },
                handled.Messages.Cast<GetOrderStatus>().Select(m => m.OrderId).Order());
        }
        finally
        {
            await vhost.SetPermissionsAsync(requesterFactory.UserName, configure: ".*", write: ".*", read: ".*");
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
        var transport = sender.Provider.GetRequiredService<IMessagingRuntime>().Transports.OfType<RabbitMQMessagingTransport>().Single();
        var replyQueue = ((RabbitMQReceiveEndpoint)transport.ReplyReceiveEndpoint!).Queue.Name;
        using var scope = sender.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new SendOptions
        {
            Endpoint = new Uri("rabbitmq:q/outbound"),
            ReplyEndpoint = customReplyAddress ? new Uri("rabbitmq://remote/q/replies") : null
        };
        await bus.SendAsync(new OrderCreated { OrderId = "before" }, options, TestContext.Current.CancellationToken);
        await vhost.SetConfigurePermissionAsync("^$");
        await vhost.CloseConnectionOfConsumerAsync(replyQueue);
        await vhost.DeleteQueueAsync(replyQueue);

        try
        {
            // act
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(s_timeout);
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
        finally
        {
            await vhost.SetConfigurePermissionAsync(".*");
        }
    }

    public sealed class StatusHandler(MessageRecorder recorder) : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return ValueTask.FromResult(new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" });
        }
    }
}
