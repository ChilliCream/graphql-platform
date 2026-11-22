using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.PublisherRecovery)]
public sealed class PublisherRecoveryTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task PublishAsync_Should_Cancel_When_BrokerBlocksPublishing()
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
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));

        // act
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(2))
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-3"),
            TestTimeouts.Signal));
    }

    [Fact]
    public async Task PublishAsync_Should_Cancel_When_BrokerRemainsOffline()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));

        // act
        await using (await fixture.Faults.StopBrokerAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(3))
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-3"),
            TestTimeouts.Signal));
    }

    [Fact]
    public async Task PublishAsync_Should_CompletePendingPublish_When_BrokerReturns()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));

        // act
        Task publish;
        await using (await fixture.Faults.StopBrokerAsync())
        {
            publish = PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
            await Assert.ThrowsAsync<TimeoutException>(
                () => publish.WaitAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken));
        }

        await publish.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-2"),
            TestTimeouts.Signal));
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_PreviousPublishWasCancelled()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.ContinuationTimeout = TimeSpan.FromSeconds(1);
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(2))
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(o => o.OrderId == "ORD-3"),
            TestTimeouts.Signal));
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_DispatchOnlyTransportRestarted()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var consumer = await CreateOrdersBusAsync(vhost, orders);
        await using var publisher = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(_ => { })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)publisher.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        await transport.StartAsync(runtime, TestContext.Current.CancellationToken);
        await PublishAsync(publisher, new OrderCreated { OrderId = "ORD-1" });
        var received = await orders.WaitAsync(TestTimeouts.Signal);

        // assert
        Assert.True(received);
    }

    [Fact]
    public async Task Consumer_Should_KeepAcking_When_PublishingConnectionBlocked()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<GatedOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<GatedOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();
        for (var i = 1; i <= 5; i++)
        {
            await PublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" });
        }

        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // act
        // the alarm blocks connections that publish, and the consumer connection only acks
        QueueInfo? whileBlocked;
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            await vhost.Broker.WaitForAlarmAsync();
            gate.Release.TrySetResult();
            await vhost.Broker.WaitForEmptyQueueAsync("orders");
            whileBlocked = await vhost.Broker.GetQueueAsync("orders");
        }

        // assert
        Assert.Equal(new QueueInfo(Consumers: 1, Ready: 0, Unacknowledged: 0), whileBlocked);
        Assert.Equal(5, orders.Messages.Count);
    }

    private static Task<TestBus> CreateOrdersBusAsync(VhostContext vhost, MessageRecorder orders)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus, OrderCreated message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(message, cts.Token);
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class GatedOrderHandler(HandlerGate gate, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            recorder.Record(message);
        }
    }
}
