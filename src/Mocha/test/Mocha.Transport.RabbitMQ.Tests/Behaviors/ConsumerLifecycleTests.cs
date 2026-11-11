using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Messaging)]
public sealed class ConsumerLifecycleTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task Consumer_Should_Recover_When_ReplyEndpointIsStopped()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var reply = transport.ReplyReceiveEndpoint!;
        await reply.StopAsync(runtime, TestContext.Current.CancellationToken);
        var before = await vhost.Broker.GetConsumerAsync("orders");

        // act
        // an endpoint attaches to a new connection by itself, whatever the state of the reply endpoint is
        await vhost.Faults.CloseConsumerConnectionAsync("orders");
        await vhost.Broker.WaitForConsumerAsync("orders", otherThan: before);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        var received = await orders.WaitAsync(TestTimeouts.Signal);

        // assert
        Assert.True(received);
        Assert.Equal(["ORD-1"], orders.Messages.Cast<OrderCreated>().Select(order => order.OrderId));
        Assert.Null(transport.ConsumerManager.HealthFailureReason);
        Assert.False(reply.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_Complete_When_HandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<StuckOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<StuckOrderHandler>())
            .BuildTestBusAsync();

        try
        {
            var runtime = bus.Provider.GetRequiredService<IMessagingRuntime>();
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
            await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

            // act
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var stop = ((MessagingRuntime)runtime).StopAsync(stopTimeout.Token).AsTask();
            await stop.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
            var requeued = await Condition.WaitAsync(
                async () => await vhost.Broker.GetQueueAsync("orders") is { Messages: 1, Consumers: 0 },
                TestTimeouts.Signal);

            // assert
            Assert.True(requeued);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task StartAsync_Should_ConsumeOnce_When_TransportStoppedBefore()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        await transport.StartAsync(runtime, TestContext.Current.CancellationToken);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        var received = await orders.WaitAsync(TestTimeouts.Signal);
        var settled = await Condition.WaitAsync(
            async () => (await vhost.Broker.GetQueueAsync("orders"))?.Messages == 0,
            TestTimeouts.Signal);
        var state = await vhost.Broker.GetStateAsync();

        // assert
        Assert.True(received);
        Assert.True(settled);
        Assert.Single(orders.Messages);
        Assert.Equal(2, state.Connections);
        Assert.Equal(2, state.Consumers);
    }

    private static async Task PublishAsync(TestBus bus, OrderCreated message)
    {
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(message, TestContext.Current.CancellationToken);
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class StuckOrderHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();

            // ignores its cancellation token, as a blocking call without a token would
            await gate.Release.Task;
        }
    }
}
