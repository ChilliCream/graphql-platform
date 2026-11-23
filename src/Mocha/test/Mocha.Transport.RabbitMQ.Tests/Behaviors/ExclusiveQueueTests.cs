using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Topology)]
public sealed class ExclusiveQueueTests(RabbitMQFixture fixture)
{
    private const string QueueName = "exclusive-orders";

    [Fact]
    public async Task StartAsync_Should_ConsumeFromExclusiveQueue_When_EndpointQueueIsExclusive()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);

        // act
        await PublishAsync(bus, "ORD-1");
        var received = await recorder.WaitAsync(TestTimeouts.Signal);
        var queue = await vhost.Broker.GetQueueAsync(QueueName);

        // assert
        Assert.True(received);
        Assert.Equal(1, queue?.Consumers);
    }

    [Fact]
    public async Task Consumer_Should_RedeclareExclusiveQueue_When_ConsumerConnectionClosed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        await PublishAsync(bus, "ORD-1");
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        var before = await vhost.Broker.GetConsumerAsync(QueueName);

        // act
        // the broker deletes an exclusive queue together with the connection that declared it
        await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
        await vhost.Broker.WaitForConsumerAsync(QueueName, otherThan: before);
        await PublishAsync(bus, "ORD-2");
        var received = await recorder.WaitAsync(TestTimeouts.Signal);

        // assert
        Assert.True(received);
        Assert.Equal(
            ["ORD-1", "ORD-2"],
            recorder.Messages.Cast<OrderCreated>().Select(order => order.OrderId).Order());
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_ExclusiveQueueHeldByAnotherBus()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);

        // act
        await using var services = CreateServices(vhost, new MessageRecorder()).Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();
        var exception = await Assert.ThrowsAnyAsync<OperationInterruptedException>(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));

        await PublishAsync(bus, "ORD-1");
        var firstStillReceives = await recorder.WaitAsync(TestTimeouts.Signal);

        // assert
        Assert.Equal<ushort?>(Constants.ResourceLocked, exception.ShutdownReason?.ReplyCode);
        Assert.True(firstStillReceives);
    }

    private static IMessageBusHostBuilder CreateServices(VhostContext vhost, MessageRecorder recorder)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue(QueueName).Exclusive();
                t.Endpoint(QueueName).Handler<OrderCreatedHandler>();
            });

    private static Task<TestBus> CreateBusAsync(VhostContext vhost, MessageRecorder recorder)
        => CreateServices(vhost, recorder).BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new OrderCreated { OrderId = orderId }, TestContext.Current.CancellationToken);
    }
}
