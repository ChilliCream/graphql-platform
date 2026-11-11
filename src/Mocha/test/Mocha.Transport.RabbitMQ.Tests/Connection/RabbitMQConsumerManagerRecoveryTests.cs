using System.Text;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public class RabbitMQConsumerManagerRecoveryTests
{
    private const string QueueName = "recovery-queue";

    // A consumer reacts to a loss at once, so a recovery that needs the general signal timeout is too slow.
    private static readonly TimeSpan s_promptly = TimeSpan.FromSeconds(10);

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

        var registration = await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
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
            maxPrefetch: 1,
            maxConcurrency: 1,
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

        var registration = await manager.RegisterConsumerAsync(
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
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None);

        await admin.PublishAsync("A");
        Assert.Equal("A", await received.NextAsync());

        // act
        await CloseChannelByBrokerAsync(registration);
        await admin.PublishAsync("B");

        // assert
        Assert.Equal("A", await received.NextAsync());
        Assert.Equal("B", await received.NextAsync());

        await abandonedHandlerCancelled.Task.WaitAsync(s_promptly, TestContext.Current.CancellationToken);
        Assert.Null(manager.HealthFailureReason);
    }

    [Fact]
    public async Task Consumer_Should_NotBeReattached_When_Disposed()
    {
        // arrange
        const string witnessQueue = "witness-queue";
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await admin.DeclareQueueAsync(witnessQueue);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();

        var registration = await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None);

        // a second consumer of the same manager shows when the consumers attached to the new connection
        await manager.RegisterConsumerAsync(
            witnessQueue,
            received.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None);
        var witness = await vhost.Broker.GetConsumerAsync(witnessQueue);

        // act
        await registration.DisposeAsync();
        var afterDispose = await vhost.Broker.GetQueueAsync(QueueName);
        await vhost.Faults.CloseConsumerConnectionAsync(witnessQueue);
        await vhost.Broker.WaitForConsumerAsync(witnessQueue, otherThan: witness);
        var afterReconnect = await vhost.Broker.GetQueueAsync(QueueName);

        // assert
        Assert.Equal(new QueueInfo(Consumers: 0, Ready: 0, Unacknowledged: 0), afterDispose);
        Assert.Equal(new QueueInfo(Consumers: 0, Ready: 0, Unacknowledged: 0), afterReconnect);
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
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None);

        // the queue stays missing, so every re-attach attempt fails and the consumer backs off
        await vhost.Faults.DeleteQueueAsync(QueueName);
        Assert.True(
            await Condition.WaitAsync(
                async () => await vhost.Broker.GetQueueAsync(QueueName) is null,
                s_promptly));

        // act
        await registration.DisposeAsync().AsTask().WaitAsync(s_promptly, TestContext.Current.CancellationToken);
        await admin.DeclareQueueAsync();

        // assert
        // nothing signals that a disposed consumer will never attach again, so this watches for a consumer for
        // longer than the first delays of a re-attach that outlived the dispose
        Assert.False(
            await Condition.WaitAsync(
                async () => (await vhost.Broker.GetQueueAsync(QueueName))?.Consumers > 0,
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
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None);

        // act
        var exception = await Assert.ThrowsAsync<OperationInterruptedException>(() => manager.RegisterConsumerAsync(
            "missing-queue",
            received.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None));
        await admin.Channel.QueueDeclareAsync(
            "missing-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // every consumer attaches to a new connection by itself, so a registration that was left behind has a
        // consumer once the other consumer of the manager is attached again
        var before = await vhost.Broker.GetConsumerAsync(QueueName);
        await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
        await vhost.Broker.WaitForConsumerAsync(QueueName, otherThan: before);
        var missingQueue = await vhost.Broker.GetQueueAsync("missing-queue");

        // assert
        Assert.Equal<ushort?>(Constants.NotFound, exception.ShutdownReason?.ReplyCode);
        Assert.Equal(new QueueInfo(Consumers: 0, Ready: 0, Unacknowledged: 0), missingQueue);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_KeepOtherConsumersAttached_When_RegistrationIsCancelled()
    {
        // arrange
        const string otherQueue = "other-queue";
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await admin.DeclareQueueAsync(otherQueue);
        await using var manager = await CreateManagerAsync(vhost);
        var received = new DeliveryRecorder();
        await manager.RegisterConsumerAsync(
            QueueName,
            received.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            CancellationToken.None);
        var original = await manager.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var before = await vhost.Broker.GetConsumerAsync(QueueName);
        var cancelledRegistrations = 0;

        // act
        // a registration is cancelled at a random moment while its channel opens and its consumer attaches
        for (var i = 0; i < 100; i++)
        {
            using var cancellation = new CancellationTokenSource();
            var registration = manager.RegisterConsumerAsync(
                otherQueue,
                received.HandleAsync,
                maxPrefetch: 1,
                maxConcurrency: 1,
                cancellation.Token);
            Thread.SpinWait(Random.Shared.Next(0, 80_000));
            await cancellation.CancelAsync();

            try
            {
                await (await registration).DisposeAsync();
            }
            catch (OperationCanceledException)
            {
                cancelledRegistrations++;
            }
        }

        var current = await manager.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var after = await vhost.Broker.GetConsumerAsync(QueueName);

        // assert
        Assert.True(cancelledRegistrations > 0, "No registration was cancelled while it was pending.");
        Assert.Same(original, current);
        Assert.Equal(before, after);
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
                maxPrefetch: 1,
                maxConcurrency: 1,
                CancellationToken.None);
        }

        var before = await vhost.Broker.GetConsumersAsync();
        var lostTags = before.Select(consumer => consumer.ConsumerTag).ToHashSet();

        // act
        await vhost.Faults.CloseConnectionsAsync();
        var after = await Wait.UntilAsync(
            vhost.Broker.GetConsumersAsync,
            consumers => consumers.Count == count && !consumers.Any(consumer => lostTags.Contains(consumer.ConsumerTag)),
            $"{count} consumers that attached after the connection was closed",
            within: s_promptly);
        var state = await vhost.Broker.GetStateAsync();

        // assert
        Assert.Equal(count, lostTags.Count);
        Assert.Equal(
            Enumerable.Range(0, count).Select(i => $"many-{i}").Order(),
            after.Select(consumer => consumer.Queue).Order());
        Assert.Equal(1, state.Connections);
        Assert.Equal(count, state.ConsumerChannels);
    }

    [Fact]
    public async Task Consumer_Should_DeliverReply_When_ReplyQueueIsRecreatedAfterConnectionLoss()
    {
        // arrange
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var admin = await AdminChannel.CreateAsync(vhost);
        await using var manager = await CreateManagerAsync(vhost);
        var replies = new DeliveryRecorder();
        var firstDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provisioning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var replyConsumer = await manager.RegisterConsumerAsync(
            "replies",
            async (_, channel, ct) =>
            {
                if (Interlocked.Increment(ref attempts) > 1)
                {
                    provisioning.TrySetResult();
                    await release.Task.WaitAsync(ct);
                }

                await channel.QueueDeclareAsync(
                    "replies",
                    durable: false,
                    exclusive: true,
                    autoDelete: true,
                    cancellationToken: ct);
            },
            replies.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            TestContext.Current.CancellationToken);
        await manager.RegisterConsumerAsync(
            QueueName,
            async (_, channel, ct) =>
            {
                await channel.QueueDeclareAsync(
                    QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: ct);
                await replyConsumer.WaitUntilAttachedAsync(ct);
            },
            async (channel, delivery, ct) =>
            {
                if (!delivery.Redelivered)
                {
                    firstDelivery.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return;
                }

                await channel.BasicPublishAsync("", "replies", delivery.Body, ct);
                await channel.BasicAckAsync(delivery.DeliveryTag, false, ct);
            },
            maxPrefetch: 1,
            maxConcurrency: 1,
            TestContext.Current.CancellationToken);
        await admin.PublishAsync("ORD-1");
        await firstDelivery.Task.WaitAsync(s_promptly, TestContext.Current.CancellationToken);

        // act
        QueueDeclareOk pending;
        try
        {
            await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
            await provisioning.Task.WaitAsync(s_promptly, TestContext.Current.CancellationToken);
            pending = await admin.Channel.QueueDeclarePassiveAsync(QueueName, TestContext.Current.CancellationToken);
        }
        finally
        {
            release.TrySetResult();
        }

        // assert
        Assert.Equal(0u, pending.ConsumerCount);
        Assert.Equal(1u, pending.MessageCount);
        Assert.Equal("ORD-1", await replies.NextAsync());
    }

    private static async Task<RabbitMQConsumerManager> CreateManagerAsync(VhostContext vhost)
    {
        var manager = TestManagers.CreateConsumerManager(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        return manager;
    }

    private static async Task CloseChannelByBrokerAsync(RabbitMQConsumer registration)
    {
        var channel = registration.Channel!;
        await channel.BasicAckAsync(deliveryTag: 9999, multiple: false, TestContext.Current.CancellationToken);

        var deadline = DateTimeOffset.UtcNow.Add(s_promptly);
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
            cts.CancelAfter(s_promptly);

            return await _bodies.Reader.ReadAsync(cts.Token);
        }
    }

    private sealed class AdminChannel(IConnection connection, IChannel channel) : IAsyncDisposable
    {
        public IChannel Channel => channel;

        public static async Task<AdminChannel> CreateAsync(VhostContext vhost)
        {
            var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
                TestContext.Current.CancellationToken);
            var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                TestContext.Current.CancellationToken);
            var admin = new AdminChannel(connection, channel);
            await admin.DeclareQueueAsync();
            return admin;
        }

        public Task DeclareQueueAsync() => DeclareQueueAsync(QueueName);

        public async Task DeclareQueueAsync(string queueName)
        {
            await channel.QueueDeclareAsync(
                queueName,
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
