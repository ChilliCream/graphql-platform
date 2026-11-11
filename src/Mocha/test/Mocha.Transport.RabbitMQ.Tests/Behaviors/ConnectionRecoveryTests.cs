using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public class ConnectionRecoveryTests
{
    private readonly RabbitMQFixture _fixture;

    public ConnectionRecoveryTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Should_ResumeReceiving_When_ConnectionRecovered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var queue = GetQueueName(bus);

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal), "Handler did not receive message 1");

        // act
        await DropConnectionsAsync(vhost, queue);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, TestContext.Current.CancellationToken);
        var received = await recorder.WaitAsync(TestTimeouts.Signal);
        await Wait.UntilAsync(() => bus.Transport.IsHealthy);

        // assert
        Assert.True(received, "Handler did not receive message 2");
        Assert.Equal(["ORD-1", "ORD-2"], GetOrderIds(recorder));
    }

    [Fact]
    public async Task Consumer_Should_HandleMultipleDisconnects_When_ConnectionDroppedRepeatedly()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var queue = GetQueueName(bus);

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal), "Handler did not receive message 1");

        // act
        await DropConnectionsAsync(vhost, queue);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, TestContext.Current.CancellationToken);
        var receivedAfterFirstDrop = await recorder.WaitAsync(TestTimeouts.Signal);
        await DropConnectionsAsync(vhost, queue);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-3" }, TestContext.Current.CancellationToken);
        var receivedAfterSecondDrop = await recorder.WaitAsync(TestTimeouts.Signal);

        // assert
        Assert.True(receivedAfterFirstDrop, "Handler did not receive message 2");
        Assert.True(receivedAfterSecondDrop, "Handler did not receive message 3");
        Assert.Equal(["ORD-1", "ORD-2", "ORD-3"], GetOrderIds(recorder));
    }

    [Fact]
    public async Task ChannelPool_Should_RecoverGracefully_When_ConnectionLost()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var queue = GetQueueName(bus);

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m },
            TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal), "Handler did not receive message 1");
        var dispatcherConnection = await bus.Transport.Dispatcher.Connection.GetConnectionAsync(
            TestContext.Current.CancellationToken);
        var dispatcherConnectionLost = dispatcherConnection.WhenShutdownAsync();

        // act
        await DropConnectionsAsync(vhost, queue);
        await dispatcherConnectionLost.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-2", Amount = 1.00m },
            TestContext.Current.CancellationToken);
        var received = await recorder.WaitAsync(TestTimeouts.Signal);

        // assert
        Assert.True(received, "Handler did not receive message 2");
        Assert.Equal(["ORD-1", "ORD-2"], recorder.Messages.Cast<ProcessPayment>().Select(p => p.OrderId).Order());
    }

    /// <summary>
    /// Closes every connection to the vhost once no delivery of <paramref name="queue"/> is in flight, and returns
    /// when the queue has a consumer that attached after the close.
    /// </summary>
    private static async Task DropConnectionsAsync(VhostContext vhost, string queue)
    {
        // a handler records its message before the acknowledgement reaches the broker
        await vhost.Broker.WaitForEmptyQueueAsync(queue);
        var before = await vhost.Broker.GetConsumerAsync(queue);

        await vhost.Faults.CloseConnectionsAsync();
        await vhost.Broker.WaitForConsumerAsync(queue, otherThan: before);
    }

    private static string GetQueueName(TestBus bus)
        => bus.Transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>()
            .Single(endpoint => endpoint.Kind == ReceiveEndpointKind.Default)
            .Queue.Name;

    private static IEnumerable<string> GetOrderIds(MessageRecorder recorder)
        => recorder.Messages.Cast<OrderCreated>().Select(order => order.OrderId).Order();

    public sealed class PaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
