using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class BrokerRecoveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_recoveryTimeout = TimeSpan.FromSeconds(20);

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
        await gate.Started.Task.WaitAsync(s_timeout, deadline.Token);

        // act
        Task publish;
        await using (await fixture.KillBrokerAsync())
        {
            await gate.Cancelled.Task.WaitAsync(s_timeout, deadline.Token);
            publish = messageBus.PublishAsync(new OrderCreated { OrderId = "during-outage" }, deadline.Token).AsTask();
            await Assert.ThrowsAsync<TimeoutException>(() => publish.WaitAsync(TimeSpan.FromMilliseconds(200), deadline.Token));
        }

        await publish.WaitAsync(s_recoveryTimeout, deadline.Token);
        var recovered = await Condition.WaitAsync(
            () => recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Distinct().Count() == 2,
            s_recoveryTimeout);
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = "after-crash" }, deadline.Token);
        var settled = await Condition.WaitAsync(async () => await vhost.GetMessageCountAsync("orders") == 0, s_timeout);

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
        await vhost.CloseConsumerConnectionsAsync();
        await AssertRecoveredAsync(vhost, bus, recorder, "ORD-2");

        // assert
        await AssertBrokerResourcesAsync(vhost, before);
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
        var broker = await fixture.StopBrokerAsync();
        await broker.DisposeAsync();
        await AssertRecoveredAsync(vhost, bus, recorder, "ORD-2");

        // assert
        await AssertBrokerResourcesAsync(vhost, before);
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

        var connection = await GetTransport(bus).ConsumerManager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // act
        // the broker stops sending heartbeats, so the client gives up on its connections after three intervals
        bool detected;
        await using (await vhost.FreezeConnectionsAsync())
        {
            detected = await Condition.WaitAsync(() => !connection.IsOpen, s_timeout);
        }

        await AssertRecoveredAsync(vhost, bus, recorder, "ORD-2");

        // assert
        Assert.True(detected);
        await AssertBrokerResourcesAsync(vhost, before);
    }

    [Fact]
    public async Task DisposeAsync_Should_LeaveNoConnection_When_BrokerUnreachableDuringRecovery()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        var bus = await CreateBusAsync(vhost, recorder);
        var transport = GetTransport(bus);
        await WarmUpAsync(vhost, bus, recorder);

        // act
        await using (await fixture.StopBrokerAsync())
        {
            await Condition.WaitAsync(() => !transport.ConsumerManager.IsConnected, s_timeout);
            await bus.DisposeAsync().AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        // a recovery that outlived the dispose would reconnect within a few recovery intervals
        var reconnected = await Condition.WaitAsync(
            async () => await vhost.GetConnectionCountAsync() > 0,
            TimeSpan.FromSeconds(5));

        // assert
        Assert.False(reconnected);
    }

    [Fact]
    public async Task Bus_Should_Reconnect_When_PasswordRotated()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-rotating", "first-password");
        await using var user = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, new UserConnectionProvider(vhost, credentials));
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        // open connections keep the old password, and the provider supplies the new one for every new connection
        await vhost.ChangePasswordAsync(credentials.UserName, "second-password");
        credentials.Password = "second-password";
        await fixture.CloseAllConnectionsAsync();
        await AssertRecoveredAsync(vhost, bus, recorder, "ORD-2");

        // assert
        await AssertBrokerResourcesAsync(vhost, before);
    }

    [Fact]
    public async Task Bus_Should_Reconnect_When_UserDeletedAndCreatedAgain()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-deleted", "password");
        await using var user = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, new UserConnectionProvider(vhost, credentials));
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        // deleting the user closes its connections, and reconnects fail with an authentication error until the
        // user exists again
        await vhost.DeleteUserAsync(credentials.UserName);
        var lost = await Condition.WaitAsync(
            async () => await vhost.GetConnectionCountAsync() == 0, s_timeout);
        await using var recreated = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        await AssertRecoveredAsync(vhost, bus, recorder, "ORD-2");

        // assert
        Assert.True(lost);
        await AssertBrokerResourcesAsync(vhost, before);
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
        for (var i = 0; i < 3; i++)
        {
            var broker = await fixture.StopBrokerAsync();
            await broker.DisposeAsync();
            await AssertRecoveredAsync(vhost, bus, recorder, $"ORD-{i + 2}");
        }

        // assert
        await AssertBrokerResourcesAsync(vhost, before);
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
        await using (await fixture.DrainNodeAsync())
        {
            closed = await Condition.WaitAsync(
                async () => await vhost.GetConnectionCountAsync() == 0,
                s_timeout);
        }

        await AssertRecoveredAsync(vhost, bus, recorder, "ORD-2");

        // assert
        Assert.True(closed);
        await AssertBrokerResourcesAsync(vhost, before);
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
        for (var i = 0; i < 5; i++)
        {
            await vhost.CloseConsumerConnectionsAsync();
            await AssertRecoveredAsync(vhost, bus, recorder, $"ORD-{i + 2}");
            await AssertBrokerResourcesAsync(vhost, before);
        }

        await bus.DisposeAsync();

        // assert
        Assert.True(await Condition.WaitAsync(async () => await vhost.GetConnectionCountAsync() == 0, s_timeout));
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

    private static RabbitMQMessagingTransport GetTransport(TestBus bus)
        => bus.Provider
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>()
            .Single();

    private static async Task AssertRecoveredAsync(
        VhostContext vhost,
        TestBus bus,
        MessageRecorder recorder,
        string orderId)
    {
        await vhost.WaitForConsumerAsync("orders", s_recoveryTimeout);
        await vhost.WaitForConsumerAsync("order-status", s_recoveryTimeout);
        await AssertRoundTripAsync(bus, recorder, orderId);
    }

    private static async Task<BrokerState> WarmUpAsync(VhostContext vhost, TestBus bus, MessageRecorder recorder)
    {
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
        return await vhost.GetStateAsync();
    }

    private static async Task AssertRoundTripAsync(TestBus bus, MessageRecorder recorder, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CreateTimeout();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = orderId }, deadline.Token);
        Assert.True(await Condition.WaitAsync(
            () => recorder.Messages.Cast<OrderCreated>().Any(order => order.OrderId == orderId), s_timeout));
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = orderId }, deadline.Token);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    private static async Task AssertBrokerResourcesAsync(VhostContext vhost, BrokerState expected)
    {
        Assert.True(await Condition.WaitAsync(async () =>
        {
            var state = await vhost.GetStateAsync();
            return state.Connections == expected.Connections
                && state.ConsumerChannels == expected.ConsumerChannels
                && state.Consumers == expected.Consumers
                && state.ConsumedQueues == expected.ConsumedQueues;
        }, s_timeout));
        var actual = await vhost.GetStateAsync();
        Assert.Equal(expected.Connections, actual.Connections);
        Assert.Equal(expected.ConsumerChannels, actual.ConsumerChannels);
        Assert.Equal(expected.Consumers, actual.Consumers);
        Assert.Equal(expected.ConsumedQueues, actual.ConsumedQueues);
    }

    private static CancellationTokenSource CreateTimeout()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
        return cts;
    }

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
