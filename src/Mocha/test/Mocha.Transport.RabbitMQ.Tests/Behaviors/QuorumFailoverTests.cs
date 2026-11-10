using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

public sealed class QuorumFailoverTests(RabbitMQClusterFixture fixture) : IClassFixture<RabbitMQClusterFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bus_Should_DeliverEventsAndReplies_When_ConnectedClusterNodeIsKilled(bool automaticRecoveryEnabled)
    {
        // arrange
        var queue = $"cluster-orders-{automaticRecoveryEnabled}";
        var primary = fixture.CreateConnectionFactory(0);
        primary.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        var secondary = fixture.CreateConnectionFactory(1);
        var provider = new ClusterConnectionProvider(primary, [primary.Endpoint, secondary.Endpoint]);
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => provider);
                t.ConfigureDefaults(defaults => defaults.Queue.QueueType = RabbitMQQueueType.Quorum);
                t.Endpoint(queue).Handler<OrderCreatedHandler>();
                t.Endpoint($"cluster-requests-{automaticRecoveryEnabled}").Handler<GetOrderStatusHandler>();
            })
            .BuildTestBusAsync();
        var transport = bus.Provider.GetRequiredService<IMessagingRuntime>().Transports
            .OfType<RabbitMQMessagingTransport>().Single();
        using var scope = bus.Provider.CreateScope();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.RequestAsync(new GetOrderStatus { OrderId = "before-failover" }, deadline.Token);
        var before = await transport.ConsumerManager.GetConnectionAsync(deadline.Token);
        Assert.Equal(primary.Port, before.Endpoint.Port);

        // act
        await using var stopped = await fixture.KillNodeAsync(0);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "after-failover" }, deadline.Token);
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = "after-failover" }, deadline.Token);
        var settled = await Condition.WaitAsync(async () =>
            !recorder.Messages.IsEmpty
                && (await fixture.GetQueueStateAsync(1, queue))?.Messages == 0
                && transport.ConsumerManager.IsHealthy,
            s_timeout);
        var after = await transport.ConsumerManager.GetConnectionAsync(deadline.Token);

        // assert
        // Recovery may redeliver messages whose acknowledgements were lost.
        Assert.Equal(["after-failover"],
            recorder.Messages.Cast<OrderCreated>().Select(message => message.OrderId).Distinct());
        Assert.True(settled);
        Assert.Equal("after-failover", response.OrderId);
        Assert.Equal(secondary.Port, after.Endpoint.Port);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bus_Should_PreserveMessagesAndKeepConsuming_When_QuorumLeaderIsKilled(bool automaticRecoveryEnabled)
    {
        // arrange
        var queue = $"orders-{automaticRecoveryEnabled}";
        var leaderFactory = fixture.CreateConnectionFactory(0);
        await using (var connection = await leaderFactory.CreateConnectionAsync(TestContext.Current.CancellationToken))
        await using (var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken))
        {
            var arguments = new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-queue-leader-locator"] = "client-local"
            };
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: arguments, cancellationToken: TestContext.Current.CancellationToken);
        }

        var before = await fixture.GetQueueStateAsync(1, queue);
        Assert.Equal(fixture.GetNodeName(0), before?.Leader);
        Assert.Equal(Enumerable.Range(0, 3).Select(fixture.GetNodeName), before!.Members.Order());
        var consumerFactory = fixture.CreateConnectionFactory(1);
        consumerFactory.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        var gate = new HandlerGate();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton<IConnectionFactory>(consumerFactory)
            .AddSingleton(gate)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<QuorumOrderHandler>()
            .AddRabbitMQ(t =>
            {
                t.ConfigureDefaults(defaults => defaults.Queue.QueueType = RabbitMQQueueType.Quorum);
                t.DeclareQueue(queue).WithArgument("x-queue-leader-locator", "client-local");
                t.Endpoint(queue).Handler<QuorumOrderHandler>().MaxConcurrency(1).MaxPrefetch(1);
            })
            .BuildTestBusAsync();
        var transport = bus.Provider.GetRequiredService<IMessagingRuntime>().Transports
            .OfType<RabbitMQMessagingTransport>().Single();
        using var scope = bus.Provider.CreateScope();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(1));
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        for (var i = 0; i < 25; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, deadline.Token);
        }

        await gate.Started.Task.WaitAsync(s_timeout, deadline.Token);

        // act
        await using var stopped = await fixture.KillNodeAsync(0);
        var elected = await Condition.WaitAsync(async () =>
            await fixture.GetQueueStateAsync(1, queue) is { Leader: { Length: > 0 } leader }
                && leader != before.Leader, s_timeout);
        gate.Release.TrySetResult();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "after-failover" }, deadline.Token);
        var settled = await Condition.WaitAsync(async () =>
            recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Distinct().Count() == 26
                && (await fixture.GetQueueStateAsync(1, queue))?.Messages == 0
                && transport.ConsumerManager.IsHealthy,
            s_timeout);

        // assert
        Assert.True(elected);
        Assert.Equal(Enumerable.Range(0, 25).Select(i => $"ORD-{i}").Append("after-failover").Order(),
            recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Distinct().Order());
        Assert.True(settled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bus_Should_RedeliverUnacknowledgedEvent_When_ConsumerNodeIsKilled(bool automaticRecoveryEnabled)
    {
        // arrange
        var queue = $"unacknowledged-orders-{automaticRecoveryEnabled}";
        var secondary = fixture.CreateConnectionFactory(1);
        await using (var connection = await secondary.CreateConnectionAsync(TestContext.Current.CancellationToken))
        await using (var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken))
        {
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-queue-leader-locator"] = "client-local"
                }, cancellationToken: TestContext.Current.CancellationToken);
        }

        var primary = fixture.CreateConnectionFactory(0);
        primary.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        var provider = new ClusterConnectionProvider(primary, [primary.Endpoint, secondary.Endpoint]);
        var gate = new HandlerGate();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(gate)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<QuorumOrderHandler>()
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => provider);
                t.ConfigureDefaults(defaults => defaults.Queue.QueueType = RabbitMQQueueType.Quorum);
                t.DeclareQueue(queue).WithArgument("x-queue-leader-locator", "client-local");
                t.Endpoint(queue).Handler<QuorumOrderHandler>().MaxConcurrency(1).MaxPrefetch(1);
            })
            .BuildTestBusAsync();
        var transport = bus.Provider.GetRequiredService<IMessagingRuntime>().Transports
            .OfType<RabbitMQMessagingTransport>().Single();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "unacknowledged" }, TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var before = await transport.ConsumerManager.GetConnectionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(primary.Port, before.Endpoint.Port);
        Assert.Equal(fixture.GetNodeName(1), (await fixture.GetQueueStateAsync(1, queue))?.Leader);

        // act
        await using var stopped = await fixture.KillNodeAsync(0);
        gate.Release.TrySetResult();
        var settled = await Condition.WaitAsync(async () =>
            !recorder.Messages.IsEmpty
                && (await fixture.GetQueueStateAsync(1, queue))?.Messages == 0
                && transport.ConsumerManager.IsHealthy,
            s_timeout);

        // assert
        Assert.True(settled);
        Assert.Equal(["unacknowledged"],
            recorder.Messages.Cast<OrderCreated>().Select(message => message.OrderId).Distinct());
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class QuorumOrderHandler(HandlerGate gate, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            recorder.Record(message);
        }
    }

    private sealed class ClusterConnectionProvider(ConnectionFactory factory, AmqpTcpEndpoint[] endpoints)
        : IRabbitMQConnectionProvider
    {
        public string Host => factory.HostName;

        public string VirtualHost => factory.VirtualHost;

        public int Port => factory.Port;

        public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
            => await factory.CreateConnectionAsync(new OrderedEndpointResolver(endpoints), null, cancellationToken);
    }

    private sealed class OrderedEndpointResolver(AmqpTcpEndpoint[] endpoints) : IEndpointResolver
    {
        public IEnumerable<AmqpTcpEndpoint> All() => endpoints;
    }
}
