using CookieCrumble;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection("RabbitMQ")]
public class RabbitMQConsumerManagerRecoveryTests
{
    private const string QueueName = "recovery-queue";

    // Shorter than the 30 second connection health check, so the consumer has to be
    // re-attached by reacting to the loss, not by the periodic health check.
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private readonly RabbitMQFixture _fixture;

    public RabbitMQConsumerManagerRecoveryTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Should_BeReattached_When_BrokerClosesChannel()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();

        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        await admin.PublishAsync("A");
        Assert.Equal("A", await received.NextAsync());

        // act
        // acking an unknown delivery tag makes the broker close the channel with 406 PRECONDITION_FAILED,
        // which is what a consumer timeout does before RabbitMQ 4.3
        await CloseChannelByBrokerAsync(registration);
        await admin.PublishAsync("B");

        // assert
        Assert.Equal("B", await received.NextAsync());
    }

    [Fact]
    public async Task Consumer_Should_BeReattached_When_BrokerCancelsConsumer()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();

        await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        await admin.PublishAsync("A");
        Assert.Equal("A", await received.NextAsync());

        // act
        // deleting the queue makes the broker send basic.cancel and keep the channel open,
        // which is what a consumer timeout does from RabbitMQ 4.3 on
        await admin.Channel.QueueDeleteAsync(QueueName, cancellationToken: TestContext.Current.CancellationToken);
        await admin.DeclareQueueAsync();
        await admin.PublishAsync("B");

        // assert
        Assert.Equal("B", await received.NextAsync());
    }

    [Fact]
    public async Task Consumer_Should_ReceiveReturnedMessageFirst_When_BrokerClosesChannelDuringHandler()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();
        var abandonedHandlerCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            QueueName,
            async (channel, eventArgs, cancellationToken) =>
            {
                // the first delivery of A blocks without being acked, as a handler retrying in place would
                if (!eventArgs.Redelivered && Encoding.UTF8.GetString(eventArgs.Body.Span) == "A")
                {
                    received.Record(eventArgs);

                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        abandonedHandlerCancelled.TrySetResult();
                    }

                    return;
                }

                await received.HandleAsync(channel, eventArgs, cancellationToken);
            },
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        await admin.PublishAsync("A");
        Assert.Equal("A", await received.NextAsync());

        // act
        await CloseChannelByBrokerAsync(registration);
        await admin.PublishAsync("B");

        // assert
        Assert.Equal("A", await received.NextAsync());
        Assert.Equal("B", await received.NextAsync());

        // the abandoned handler keeps running until the consumer is disposed, which cancels it
        Assert.False(abandonedHandlerCancelled.Task.IsCompleted);
        await registration.DisposeAsync();
        await abandonedHandlerCancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Consumer_Should_NotBeReattached_When_Disposed()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();

        var registration = await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        // act
        await registration.DisposeAsync();
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // assert
        var queue = await admin.Channel.QueueDeclarePassiveAsync(
            QueueName,
            TestContext.Current.CancellationToken);
        Assert.Equal(0u, queue.ConsumerCount);
    }

    [Fact]
    public async Task Consumer_Should_StayDetached_When_DisposedDuringReattachBackoff()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();

        var registration = await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        // the queue stays missing, so every re-attach attempt fails and the consumer backs off
        await vhost.DeleteQueueAsync(QueueName);
        Assert.True(
            await Condition.WaitAsync(
                async () => !(await vhost.GetConsumerCountsAsync()).ContainsKey(QueueName),
                s_timeout));

        // act
        await registration.DisposeAsync().AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await admin.DeclareQueueAsync();

        // assert
        // a re-attach that outlived the dispose would subscribe within its next backoff of at most two seconds
        Assert.False(
            await Condition.WaitAsync(
                async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault(QueueName) > 0,
                TimeSpan.FromSeconds(4)));
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_LeaveNoConsumerBehind_When_FirstAttachFails()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();
        await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        // act
        var exception = await Record.ExceptionAsync(() => manager.RegisterConsumerAsync(
            "missing-queue",
            received.HandleAsync,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            CancellationToken.None));
        await admin.Channel.QueueDeclareAsync(
            "missing-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // a new connection re-attaches every registered consumer
        await _fixture.CloseAllConnectionsAsync();
        var reattached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault(QueueName) == 1,
            s_timeout);
        var zombie = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("missing-queue") > 0,
            TimeSpan.FromSeconds(3));

        // assert
        Assert.IsType<OperationInterruptedException>(exception);
        Assert.True(reattached);
        Assert.False(zombie);
    }

    [Fact]
    public async Task Consumers_Should_AllReattach_When_ConnectionClosedWithManyQueues()
    {
        // arrange
        const int count = 50;
        await using var vhost = await _fixture.CreateVhostAsync();
        await using (var admin = await AdminChannel.CreateAsync(vhost))
        {
            for (var i = 0; i < count; i++)
            {
                await admin.Channel.QueueDeclareAsync(
                    $"many-{i}",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: TestContext.Current.CancellationToken);
            }
        }

        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();
        for (var i = 0; i < count; i++)
        {
            await manager.RegisterConsumerAsync(
                $"many-{i}",
                received.HandleAsync,
                prefetchCount: 1,
                consumerDispatchConcurrency: 1,
                CancellationToken.None);
        }

        // act
        await _fixture.CloseAllConnectionsAsync();
        var reattached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).Count(queue => queue.Value == 1) == count,
            TimeSpan.FromSeconds(20));
        var state = await vhost.GetStateAsync();

        // assert
        new { Reattached = reattached, state.Connections, state.ConsumerChannels }.MatchInlineSnapshot(
            """
            {
              "Reattached": true,
              "Connections": 1,
              "ConsumerChannels": 50
            }
            """);
    }

    private static async Task<RabbitMQConsumerManager> CreateManagerAsync(VhostContext vhost)
    {
        var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        return manager;
    }

    private static async Task CloseChannelByBrokerAsync(RabbitMQConsumerManager.RegisteredConsumer registration)
    {
        var channel = registration.Channel!;
        await channel.BasicAckAsync(deliveryTag: 9999, multiple: false, TestContext.Current.CancellationToken);

        var deadline = DateTimeOffset.UtcNow.Add(s_timeout);
        while (channel.IsOpen && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.False(channel.IsOpen, "The broker did not close the consumer channel.");
    }

    private sealed class DeliveryRecorder
    {
        private readonly System.Threading.Channels.Channel<string> _bodies =
            System.Threading.Channels.Channel.CreateUnbounded<string>();

        public ValueTask HandleAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken _)
        {
            Record(eventArgs);
            return channel.BasicAckAsync(eventArgs.DeliveryTag, false, CancellationToken.None);
        }

        public void Record(BasicDeliverEventArgs eventArgs)
            => _bodies.Writer.TryWrite(Encoding.UTF8.GetString(eventArgs.Body.Span));

        public async Task<string> NextAsync()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cts.CancelAfter(s_timeout);

            try
            {
                return await _bodies.Reader.ReadAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return "<no message received>";
            }
        }
    }

    private sealed class AdminChannel(IConnection connection, IChannel channel) : IAsyncDisposable
    {
        public IChannel Channel => channel;

        public static async Task<AdminChannel> CreateAsync(VhostContext vhost)
        {
            var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
                TestContext.Current.CancellationToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
            var admin = new AdminChannel(connection, channel);
            await admin.DeclareQueueAsync();
            return admin;
        }

        public async Task DeclareQueueAsync()
        {
            await channel.QueueDeclareAsync(
                QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: TestContext.Current.CancellationToken);
        }

        public async Task PublishAsync(string body)
        {
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                body: Encoding.UTF8.GetBytes(body),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await channel.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
