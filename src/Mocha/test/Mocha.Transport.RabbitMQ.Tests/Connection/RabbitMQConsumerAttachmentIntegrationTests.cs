using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public sealed class RabbitMQConsumerAttachmentIntegrationTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Consumer_Should_Recover_When_ChannelOpenTimesOut(bool automaticRecoveryEnabled)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        vhost.ConnectionSettings.ContinuationTimeout = TimeSpan.FromSeconds(1);
        vhost.ConnectionSettings.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            TestContext.Current.CancellationToken);
        await DeclareQueueAsync(channel, "orders");
        await channel.BasicPublishAsync("", "orders", "retained"u8.ToArray(), TestContext.Current.CancellationToken);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(vhost);
        var original = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // act
        await using (await vhost.FreezeConnectionsAsync())
        {
            await manager.RegisterConsumerAsync(
                "orders",
                (consumerChannel, delivery, ct) => RecordDeliveryAsync(received, consumerChannel, delivery, ct),
                prefetchCount: 1,
                consumerDispatchConcurrency: 1,
                TestContext.Current.CancellationToken);
        }

        var delivered = await received.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var current = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("retained", delivered);
        Assert.NotSame(original, current);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_PreserveOtherConsumers_When_ChannelLimitReached()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            TestContext.Current.CancellationToken);
        await DeclareQueueAsync(channel, "orders");
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(vhost);
        var original = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);
        await manager.RegisterConsumerAsync(
            "orders",
            (consumerChannel, delivery, ct) => RecordDeliveryAsync(received, consumerChannel, delivery, ct),
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // act
        await Assert.ThrowsAsync<ChannelAllocationException>(() => manager.RegisterConsumerAsync(
            "another-queue",
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken));
        await channel.BasicPublishAsync("", "orders", "available"u8.ToArray(), TestContext.Current.CancellationToken);
        var delivered = await received.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var current = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("available", delivered);
        Assert.Same(original, current);
    }

    [Fact]
    public async Task Consumer_Should_DeliverFromOtherQueue_When_OneTopologyTimesOut()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            TestContext.Current.CancellationToken);
        await DeclareQueueAsync(channel, "slow");
        await DeclareQueueAsync(channel, "healthy");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowDelivery = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var healthyDelivery = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(vhost);
        manager.AttachTimeout = TimeSpan.FromSeconds(1);
        await manager.RegisterConsumerAsync(
            "slow",
            async (_, consumerChannel, ct) =>
            {
                await release.Task.WaitAsync(ct);
                await DeclareQueueAsync(consumerChannel, "slow");
            },
            dependencies: [],
            (consumerChannel, delivery, ct) => RecordDeliveryAsync(slowDelivery, consumerChannel, delivery, ct),
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        await manager.RegisterConsumerAsync(
            "healthy",
            (consumerChannel, delivery, ct) => RecordDeliveryAsync(healthyDelivery, consumerChannel, delivery, ct),
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // act
        try
        {
            await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken)
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await channel.BasicPublishAsync("", "healthy", "available"u8.ToArray(), TestContext.Current.CancellationToken);
            await channel.BasicPublishAsync("", "slow", "retained"u8.ToArray(), TestContext.Current.CancellationToken);

            // assert
            Assert.Equal("available", await healthyDelivery.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1u, (await channel.QueueDeclarePassiveAsync("slow", TestContext.Current.CancellationToken)).MessageCount);
        }
        finally
        {
            release.TrySetResult();
        }

        Assert.Equal("retained", await slowDelivery.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Consumer_Should_WaitForReplyQueue_When_DependencyTopologyIsDelayed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            TestContext.Current.CancellationToken);
        await DeclareQueueAsync(channel, "requests");
        var replyStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(vhost);
        var replies = await manager.RegisterConsumerAsync(
            "replies",
            async (_, consumerChannel, ct) =>
            {
                replyStarted.TrySetResult();
                await release.Task.WaitAsync(ct);
                await DeclareQueueAsync(consumerChannel, "replies");
            },
            dependencies: [],
            (consumerChannel, delivery, ct) => RecordDeliveryAsync(response, consumerChannel, delivery, ct),
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        await manager.RegisterConsumerAsync(
            "requests",
            (_, _, _) =>
            {
                requestStarted.TrySetResult();
                return Task.CompletedTask;
            },
            dependencies: [() => replies],
            async (consumerChannel, delivery, ct) =>
            {
                await consumerChannel.BasicPublishAsync("", "replies", delivery.Body, ct);
                await consumerChannel.BasicAckAsync(delivery.DeliveryTag, false, ct);
            },
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // act
        var connecting = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        QueueDeclareOk pending;
        try
        {
            await Task.WhenAll(replyStarted.Task, requestStarted.Task)
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await channel.BasicPublishAsync("", "requests", "ORD-1"u8.ToArray(), TestContext.Current.CancellationToken);
            pending = await channel.QueueDeclarePassiveAsync("requests", TestContext.Current.CancellationToken);
        }
        finally
        {
            release.TrySetResult();
            await connecting.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.Equal(0u, pending.ConsumerCount);
        Assert.Equal(1u, pending.MessageCount);
        Assert.Equal("ORD-1", await response.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StopAsync_Should_RetainQueuedMessage_When_ConsumerStopsDuringProvisioning()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            TestContext.Current.CancellationToken);
        await DeclareQueueAsync(channel, "orders");
        await channel.BasicPublishAsync("", "orders", "ORD-1"u8.ToArray(), TestContext.Current.CancellationToken);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(vhost);
        var consumer = await manager.RegisterConsumerAsync(
            "orders",
            async (_, _, ct) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(ct);
            },
            dependencies: [],
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // act
        var connecting = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        try
        {
            await started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await consumer.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            release.TrySetResult();
            await connecting.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        var queue = await channel.QueueDeclarePassiveAsync("orders", TestContext.Current.CancellationToken);
        var retained = await channel.BasicGetAsync("orders", true, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0u, queue.ConsumerCount);
        Assert.Equal(1u, queue.MessageCount);
        Assert.Equal("ORD-1", Encoding.UTF8.GetString(retained!.Body.Span));
    }

    private static RabbitMQConsumerManager CreateManager(VhostContext vhost)
        => new(
            NullLogger<RabbitMQConsumerManager>.Instance,
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));

    private static async Task DeclareQueueAsync(IChannel channel, string queue)
        => await channel.QueueDeclareAsync(
            queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);

    private static async ValueTask RecordDeliveryAsync(
        TaskCompletionSource<string> received,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        var value = Encoding.UTF8.GetString(delivery.Body.Span);
        await channel.BasicAckAsync(delivery.DeliveryTag, false, cancellationToken);
        received.TrySetResult(value);
    }
}
