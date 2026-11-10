using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class PublisherRecoveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

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
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        await using (await fixture.BlockPublishersAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(2))
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-3"),
            s_timeout));
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_ConnectionClosedWhileAwaitingConfirm()
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
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        Task publish;
        await using (await fixture.BlockPublishersAsync())
        {
            publish = PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(30));
            await vhost.WaitForBlockedPublisherAsync(s_timeout);
            await fixture.CloseAllConnectionsAsync();
        }

        await publish.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-2"),
            s_timeout));
    }

    [Fact]
    public async Task PublishAsync_Should_Cancel_When_BrokerRemainsOffline()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        await using (await fixture.StopBrokerAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(3))
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-3"),
            s_timeout));
    }

    [Fact]
    public async Task PublishAsync_Should_CompletePendingPublish_When_BrokerReturns()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        Task publish;
        await using (await fixture.StopBrokerAsync())
        {
            publish = PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(30));
            await Assert.ThrowsAsync<TimeoutException>(
                () => publish.WaitAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken));
        }

        await publish.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(order => order.OrderId == "ORD-2"),
            s_timeout));
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverEveryMessage_When_ConnectionDropsDuringConcurrentPublishes()
    {
        // arrange
        const int count = 500;
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-0" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        Task[] publishes;
        await using (await fixture.BlockPublishersAsync())
        {
            publishes = Enumerable.Range(1, count)
                .Select(i => PublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" }, TimeSpan.FromSeconds(30)))
                .ToArray();
            await vhost.WaitForBlockedPublisherAsync(s_timeout);
            await fixture.CloseAllConnectionsAsync();
        }

        await Task.WhenAll(publishes).WaitAsync(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Select(o => o.OrderId).Distinct().Count() == count + 1,
            TimeSpan.FromSeconds(30)));
        Assert.Equal(
            Enumerable.Range(0, count + 1).Select(i => $"ORD-{i}").Order(),
            orders.Messages.Cast<OrderCreated>().Select(o => o.OrderId).Distinct().Order());
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
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        await using (await vhost.FreezeConnectionsAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(2))
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => orders.Messages.Cast<OrderCreated>().Any(o => o.OrderId == "ORD-3"),
            s_timeout));
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
        var received = await orders.WaitAsync(s_timeout);

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

        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        // the alarm blocks connections that publish, and the consumer connection only acks
        bool drained;
        await using (await fixture.BlockPublishersAsync())
        {
            gate.Release.TrySetResult();
            drained = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("orders") == 0,
                s_timeout);
        }

        // assert
        Assert.True(drained);
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
        cts.CancelAfter(timeout ?? s_timeout);
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
