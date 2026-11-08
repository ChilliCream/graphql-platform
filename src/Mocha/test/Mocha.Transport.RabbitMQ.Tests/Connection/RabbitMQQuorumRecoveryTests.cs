using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public sealed class RabbitMQQuorumRecoveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Consumer_Should_RetainUnprocessedMessages_When_LostHandlerUsesAllCapacity()
    {
        // arrange
        await using var timeout = await fixture.SetConsumerTimeoutAsync(TimeSpan.FromSeconds(1));
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum", ["x-delivery-limit"] = 2 },
            cancellationToken: TestContext.Current.CancellationToken);
        await using var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocations = 0;
        var delivered = new ConcurrentQueue<string>();
        await manager.RegisterConsumerAsync("orders", async (consumerChannel, delivery, ct) =>
        {
            if (Interlocked.Increment(ref invocations) == 1)
            {
                await using var registration = ct.Register(() => cancelled.TrySetResult());
                started.TrySetResult();
                await release.Task;
                ct.ThrowIfCancellationRequested();
            }

            delivered.Enqueue(Encoding.UTF8.GetString(delivery.Body.Span));
            await consumerChannel.BasicAckAsync(delivery.DeliveryTag, false, ct);
        }, 2, 1, TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "first"u8.ToArray(), TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "second"u8.ToArray(), TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        try
        {
            await cancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await vhost.WaitForNoConsumerAsync("orders", s_timeout);
            // This spans several broker acknowledgement deadlines and exceeds the delivery limit if reattached.
            await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var whileBlocked = (await vhost.GetConsumerCountAsync("orders"),
                await vhost.GetMessageCountAsync("orders"), invocations, manager.IsHealthy);
            release.TrySetResult();
            var settled = await Condition.WaitAsync(
                async () => delivered.Count == 2 && await vhost.GetMessageCountAsync("orders") == 0, s_timeout);

            // assert
            Assert.Equal((0, 2, 1, false), whileBlocked);
            Assert.True(settled);
            Assert.Equal(["first", "second"], delivered.Order());
            Assert.True(manager.IsHealthy);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Theory]
    [InlineData(-1, 25, false)]
    [InlineData(-1, 25, true)]
    [InlineData(3, 4, false)]
    [InlineData(3, 4, true)]
#if RABBITMQ_3
    [InlineData(null, 25, false)]
    [InlineData(null, 25, true)]
#else
    [InlineData(null, 21, false)]
    [InlineData(null, 21, true)]
#endif
    public async Task QuorumQueue_Should_RespectDeliveryLimit_When_ChannelsCloseRepeatedly(
        int? limit,
        int expectedDeliveries,
        bool useBusDefault)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                var queue = t.DeclareQueue("orders");
                if (useBusDefault)
                {
                    t.ConfigureDefaults(d =>
                    {
                        d.Queue.QueueType = RabbitMQQueueType.Quorum;
                        if (limit is { } deliveryLimit)
                        {
                            d.Queue.Arguments["x-delivery-limit"] = deliveryLimit;
                        }
                    });
                }
                else
                {
                    queue.QueueType(RabbitMQQueueType.Quorum);
                    if (limit is { } deliveryLimit)
                    {
                        queue.MaxDeliveryLimit(deliveryLimit);
                    }
                }
            })
            .BuildTestBusAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var publisher = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await publisher.BasicPublishAsync("", "orders", "order"u8.ToArray(), TestContext.Current.CancellationToken);

        // act
        var deliveries = new List<string>();
        for (var i = 0; i < 25; i++)
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
            BasicGetResult? delivery = null;
            var available = await Condition.WaitAsync(async () =>
            {
                delivery = await channel.BasicGetAsync("orders", false, TestContext.Current.CancellationToken);
                return delivery is not null;
            }, TimeSpan.FromSeconds(2));
            if (!available)
            {
                break;
            }

            deliveries.Add(Encoding.UTF8.GetString(delivery!.Body.Span));
            if (i == 24)
            {
                await channel.BasicAckAsync(delivery.DeliveryTag, false, TestContext.Current.CancellationToken);
            }
        }

        var settled = await Condition.WaitAsync(async () => await vhost.GetMessageCountAsync("orders") == 0, s_timeout);

        // assert
        Assert.Equal(Enumerable.Repeat("order", expectedDeliveries), deliveries);
        Assert.True(settled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_Should_PreserveMessages_When_ExistingQuorumQueueHasBrokerDeliveryLimit(
        bool useVirtualHostDefault)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        if (useVirtualHostDefault)
        {
            await fixture.InvokeCommandAsync(
                ["rabbitmqctl", "update_vhost_metadata", vhost.VhostName, "--default-queue-type", "quorum"]);
        }

        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "retained"u8.ToArray(), TestContext.Current.CancellationToken);
        var builder = new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                var queue = t.DeclareQueue("orders");
                if (!useVirtualHostDefault)
                {
                    queue.QueueType(RabbitMQQueueType.Quorum);
                }
            });

        // act
        await using var bus = await builder.BuildTestBusAsync();
        var delivery = await channel.BasicGetAsync("orders", true, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("retained", Encoding.UTF8.GetString(delivery!.Body.Span));
        Assert.Equal(0u, await channel.MessageCountAsync("orders", TestContext.Current.CancellationToken));
    }
}
