using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class MissingQueueRecoveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Handler_Should_ReceiveEvent_When_TemporaryQueueWasDeletedWithConsumerConnection()
    {
        // arrange
        const string queueName = "temporary-orders";
        var recorder = new MessageRecorder();
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(queueName).Handler<OrderCreatedHandler>().Temporary())
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        // the temporary queue is auto-delete, so closing its consumer connection deletes it and its binding
        await vhost.CloseConsumerConnectionAsync(queueName);
        await vhost.WaitForConsumerAsync(queueName, s_timeout);

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_DeclareQueue_When_QueueDoesNotExist()
    {
        // arrange
        const string queueName = "missing-queue";
        await using var vhost = await fixture.CreateVhostAsync();
        await using var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            ct => new ValueTask<IConnection>(vhost.ConnectionFactory.CreateConnectionAsync(ct)));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        await using var consumer = await manager.RegisterConsumerAsync(
            queueName,
            (_, _, _) => default,
            async (channel, ct) => await channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct),
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, await vhost.GetConsumerCountAsync(queueName));
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_Throw_When_QueueDoesNotExistAndNoProvisioning()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            ct => new ValueTask<IConnection>(vhost.ConnectionFactory.CreateConnectionAsync(ct)));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<OperationInterruptedException>(() => manager.RegisterConsumerAsync(
            "missing-queue",
            (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(Constants.NotFound, (int?)exception.ShutdownReason?.ReplyCode);
    }
}
