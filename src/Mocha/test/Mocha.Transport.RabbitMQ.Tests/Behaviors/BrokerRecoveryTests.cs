using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.BrokerRecovery)]
public sealed class BrokerRecoveryTests(RabbitMQFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bus_Should_RedeliverAndResumePublishing_When_BrokerProcessIsKilled(bool automaticRecoveryEnabled)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        var gate = new CrashHandlerGate();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<CrashOrderHandler>()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("orders").Handler<CrashOrderHandler>();
                t.Endpoint("order-status").Handler<GetOrderStatusHandler>();
            })
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "before-crash" }, deadline.Token);
        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, deadline.Token);

        // act
        Task publish;
        await using (await fixture.Faults.KillBrokerAsync())
        {
            await gate.Cancelled.Task.WaitAsync(TestTimeouts.Signal, deadline.Token);
            publish = messageBus.PublishAsync(new OrderCreated { OrderId = "during-outage" }, deadline.Token).AsTask();
            await Assert.ThrowsAsync<TimeoutException>(() => publish.WaitAsync(TimeSpan.FromMilliseconds(200), deadline.Token));
        }

        await publish.WaitAsync(TestTimeouts.Signal, deadline.Token);
        var recovered = await Condition.WaitAsync(
            () => recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Distinct().Count() == 2,
            TestTimeouts.Signal);
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = "after-crash" }, deadline.Token);
        var settled = await Condition.WaitAsync(
            async () => (await vhost.Broker.GetQueueAsync("orders"))?.Messages == 0,
            TestTimeouts.Signal);

        // assert
        Assert.True(recovered);
        Assert.Equal(["before-crash", "during-outage"],
            recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Distinct().Order());
        Assert.Equal("after-crash", response.OrderId);
        Assert.True(settled);
    }

    [Fact]
    public async Task Bus_Should_DeliverEventsAndReplies_When_BrokerClosesConsumerConnection()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        await vhost.Faults.CloseConsumerConnectionAsync("orders");
        await AssertRecoveredAsync(vhost, bus, recorder, before.Consumers, "ORD-2");

        // assert
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task Bus_Should_DeliverEventsAndReplies_When_BrokerRestarts()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        var broker = await fixture.Faults.StopBrokerAsync();
        await broker.DisposeAsync();
        await AssertRecoveredAsync(vhost, bus, recorder, before.Consumers, "ORD-2");

        // assert
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task Bus_Should_DeliverEventsAndReplies_When_HeartbeatsAreMissed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedHeartbeat = TimeSpan.FromSeconds(1);
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        var connection = await bus.Transport.ConsumerManager.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // act
        // the broker stops sending heartbeats, so the client gives up on its connections after three intervals
        bool detected;
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            detected = await Condition.WaitAsync(() => !connection.IsOpen, TestTimeouts.Signal);
        }

        await AssertRecoveredAsync(vhost, bus, recorder, before.Consumers, "ORD-2");

        // assert
        Assert.True(detected);
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task DisposeAsync_Should_LeaveNoConnection_When_BrokerUnreachableDuringRecovery()
    {
        // arrange
        // a recovery waits at most this long between two attempts to connect
        var longestReconnectDelay = TimeSpan.FromSeconds(1);
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        var bus = await CreateBusAsync(vhost, recorder);
        var transport = bus.Transport;
        transport.ConsumerManager.Connection.MaxRetryDelay = longestReconnectDelay;
        transport.Dispatcher.Connection.MaxRetryDelay = longestReconnectDelay;
        await WarmUpAsync(vhost, bus, recorder);

        // act
        await using (await fixture.Faults.StopBrokerAsync())
        {
            await Wait.UntilAsync(() => !transport.ConsumerManager.IsConnected);
            await bus.DisposeAsync().AsTask().WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        }

        // nothing signals that a disposed bus will never connect again, so this watches for a connection for
        // longer than a recovery that outlived the dispose would wait before its next attempt
        var reconnected = await Condition.WaitAsync(
            async () => await vhost.Broker.GetConnectionCountAsync() > 0,
            longestReconnectDelay + TimeSpan.FromSeconds(2));

        // assert
        Assert.False(reconnected);
    }

    [Fact]
    public async Task Bus_Should_Reconnect_When_PasswordRotated()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-rotating", "first-password");
        await using var user = await vhost.Faults.AddUserAsync(credentials.UserName, credentials.Password);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, new UserConnectionProvider(vhost, credentials));
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        // open connections keep the old password, and the provider supplies the new one for every new connection
        await vhost.Faults.ChangePasswordAsync(credentials.UserName, "second-password");
        credentials.Password = "second-password";
        await vhost.Faults.CloseConnectionsAsync();
        await AssertRecoveredAsync(vhost, bus, recorder, before.Consumers, "ORD-2");

        // assert
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task Bus_Should_Reconnect_When_UserDeletedAndCreatedAgain()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-deleted", "password");
        await using var user = await vhost.Faults.AddUserAsync(credentials.UserName, credentials.Password);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, new UserConnectionProvider(vhost, credentials));
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        // deleting the user closes its connections, and reconnects fail with an authentication error until the
        // user exists again
        await vhost.Faults.DeleteUserAsync(credentials.UserName);
        var lost = await Condition.WaitAsync(
            async () => await vhost.Broker.GetConnectionCountAsync() == 0,
            TestTimeouts.Signal);
        await using var recreated = await vhost.Faults.AddUserAsync(credentials.UserName, credentials.Password);
        await AssertRecoveredAsync(vhost, bus, recorder, before.Consumers, "ORD-2");

        // assert
        Assert.True(lost);
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task Bus_Should_DeliverEventsAndReplies_When_BrokerRestartsRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        var consumers = before.Consumers;
        for (var i = 0; i < 3; i++)
        {
            var broker = await fixture.Faults.StopBrokerAsync();
            await broker.DisposeAsync();
            consumers = await AssertRecoveredAsync(vhost, bus, recorder, consumers, $"ORD-{i + 2}");
        }

        // assert
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task Bus_Should_Resume_When_NodeDrainedAndRevived()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        // maintenance mode closes every client connection with 320 and refuses new ones until the node is revived
        bool closed;
        await using (await fixture.Faults.DrainNodeAsync())
        {
            closed = await Condition.WaitAsync(
                async () => await vhost.Broker.GetConnectionCountAsync() == 0,
                TestTimeouts.Signal);
        }

        await AssertRecoveredAsync(vhost, bus, recorder, before.Consumers, "ORD-2");

        // assert
        Assert.True(closed);
        await AssertBrokerResourcesAsync(vhost, before.State);
    }

    [Fact]
    public async Task Bus_Should_NotLeakConsumerResources_When_ConsumerConnectionClosesRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        var consumers = before.Consumers;
        for (var i = 0; i < 5; i++)
        {
            await vhost.Faults.CloseConsumerConnectionAsync("orders");
            consumers = await AssertRecoveredAsync(vhost, bus, recorder, consumers, $"ORD-{i + 2}");
            await AssertBrokerResourcesAsync(vhost, before.State);
        }

        await bus.DisposeAsync();

        // assert
        await vhost.Broker.WaitForNoConnectionAsync();
    }

    private static Task<TestBus> CreateBusAsync(
        VhostContext vhost,
        MessageRecorder recorder,
        IRabbitMQConnectionProvider? connectionProvider = null)
        => CreateServices(vhost, recorder, connectionProvider).BuildTestBusAsync();

    private static IMessageBusHostBuilder CreateServices(
        VhostContext vhost,
        MessageRecorder recorder,
        IRabbitMQConnectionProvider? connectionProvider = null)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t =>
            {
                if (connectionProvider is not null)
                {
                    t.ConnectionProvider(_ => connectionProvider);
                }

                t.Endpoint("orders").Handler<OrderCreatedHandler>();
                t.Endpoint("order-status").Handler<GetOrderStatusHandler>();
            });

    /// <summary>
    /// Waits until both endpoints have a consumer that is not one of <paramref name="lost"/>, sends an event and
    /// a request through the bus, and returns the new consumers.
    /// </summary>
    private static async Task<IReadOnlyList<ConsumerInfo>> AssertRecoveredAsync(
        VhostContext vhost,
        TestBus bus,
        MessageRecorder recorder,
        IReadOnlyList<ConsumerInfo> lost,
        string orderId)
    {
        var orders = await vhost.Broker.WaitForConsumerAsync(
            "orders",
            otherThan: lost.Single(consumer => consumer.Queue == "orders"));
        var orderStatus = await vhost.Broker.WaitForConsumerAsync(
            "order-status",
            otherThan: lost.Single(consumer => consumer.Queue == "order-status"));
        await AssertRoundTripAsync(bus, recorder, orderId);

        return [orders, orderStatus];
    }

    private static async Task<Baseline> WarmUpAsync(VhostContext vhost, TestBus bus, MessageRecorder recorder)
    {
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
        return new Baseline(await vhost.Broker.GetStateAsync(), await vhost.Broker.GetConsumersAsync());
    }

    private static async Task AssertRoundTripAsync(TestBus bus, MessageRecorder recorder, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CreateTimeout();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = orderId }, deadline.Token);
        await Wait.UntilAsync(() => recorder.Messages.Cast<OrderCreated>().Any(order => order.OrderId == orderId));
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = orderId }, deadline.Token);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    private static async Task AssertBrokerResourcesAsync(VhostContext vhost, BrokerState expected)
        => await Wait.UntilAsync(
            vhost.Broker.GetStateAsync,
            state => state with { IdleChannels = expected.IdleChannels } == expected,
            $"the broker to hold the connections, consumer channels and consumers of {expected}");

    private static CancellationTokenSource CreateTimeout()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TestTimeouts.Signal);
        return cts;
    }

    private sealed record Baseline(BrokerState State, IReadOnlyList<ConsumerInfo> Consumers);

    public sealed class CrashHandlerGate
    {
        public int Invocations;

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class CrashOrderHandler(CrashHandlerGate gate, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref gate.Invocations) == 1)
            {
                gate.Started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    gate.Cancelled.TrySetResult();
                    throw;
                }
            }

            recorder.Record(message);
        }
    }

    private sealed class Credentials(string userName, string password)
    {
        public string UserName => userName;

        public string Password { get; set; } = password;
    }

    private sealed class UserConnectionProvider(VhostContext vhost, Credentials credentials)
        : IRabbitMQConnectionProvider
    {
        public string Host => vhost.ConnectionSettings.HostName;

        public string VirtualHost => vhost.VhostName;

        public int Port => vhost.ConnectionSettings.Port;

        public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                Uri = vhost.ConnectionSettings.Uri,
                VirtualHost = vhost.VhostName,
                UserName = credentials.UserName,
                Password = credentials.Password
            };

            return await factory.CreateConnectionAsync(cancellationToken);
        }
    }
}
