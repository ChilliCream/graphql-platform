using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class ConnectionStartupTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task StartAsync_Should_Complete_When_BrokerBecomesReachableDuringStart()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var broker = await fixture.Faults.StopBrokerAsync();
        Task<TestBus> start;

        // act
        try
        {
            start = CreateBusAsync(vhost, recorder);
            await Assert.ThrowsAsync<TimeoutException>(
                () => start.WaitAsync(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken));
        }
        finally
        {
            await broker.DisposeAsync();
        }

        await using var bus = await start.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_VirtualHostAccessRefused()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var revoked = await vhost.Faults.RevokeAccessAsync();

        // act
        // the refusal arrives after the handshake, while or before the first request on the connection is sent
        var exception = await Assert.ThrowsAnyAsync<OperationInterruptedException>(
            () => StartAndDisposeAsync(CreateServices(vhost, recorder)));

        // assert
        Assert.Equal<ushort?>(Constants.NotAllowed, exception.ShutdownReason?.ReplyCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_Should_FailFast_When_CredentialsRefused(bool infiniteStartTimeout)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.Password = "wrong-password";
        var recorder = new MessageRecorder();
        var builder = CreateServices(
            vhost,
            recorder,
            startTimeout: infiniteStartTimeout ? Timeout.InfiniteTimeSpan : null);

        // act
        var exception = await Assert.ThrowsAsync<BrokerUnreachableException>(
            () => StartAndDisposeAsync(builder));

        // assert
        Assert.IsType<AuthenticationFailureException>(exception.InnerException);
        Assert.Equal(0, await vhost.Broker.GetConnectionCountAsync());
    }

    [Fact]
    public async Task StartAsync_Should_FailFastWithoutLeakingConnections_When_ConfigurePermissionDenied()
    {
        // arrange
        // the user may open the vhost and publish, but declares nothing
        await using var vhost = await fixture.CreateVhostAsync();
        await vhost.Faults.SetPermissionsAsync("guest", configure: "^$", write: ".*", read: ".*");
        var recorder = new MessageRecorder();

        // act
        var exception = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => StartAndDisposeAsync(CreateServices(vhost, recorder)));

        // assert
        Assert.Equal<ushort?>(Constants.AccessRefused, exception.ShutdownReason?.ReplyCode);
        Assert.True(await Condition.WaitAsync(async () => await vhost.Broker.GetConnectionCountAsync() == 0, TestTimeouts.Signal));
    }

    [Fact]
    public async Task StartAsync_Should_Fail_When_BrokerUnreachableForStartTimeout()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.HostName = "mocha-unreachable.invalid";
        await using var services = new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.StartTimeout(TimeSpan.FromSeconds(3));
                t.Endpoint("orders").Handler<OrderCreatedHandler>();
            })
            .Services
            .BuildServiceProvider();
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();

        // act
        await Assert.ThrowsAsync<BrokerUnreachableException>(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));

        // assert
        Assert.False(runtime.IsStarted);
        Assert.Equal(0, await vhost.Broker.GetConnectionCountAsync());
    }

    [Fact]
    public async Task StartAsync_Should_Complete_When_StartTimeoutIsZero()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();

        // act
        // a timeout of zero allows one attempt and no retry
        await using var bus = await CreateServices(vhost, recorder, startTimeout: TimeSpan.Zero).BuildTestBusAsync();

        // assert
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
    }

    [Fact]
    public async Task StartAsync_Should_KeepRetryingUntilBrokerReturns_When_StartTimeoutIsInfinite()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var connectionProvider = new CountingConnectionProvider(vhost);
        var broker = await fixture.Faults.StopBrokerAsync();
        Task<TestBus> start;
        bool pendingAfterFailedAttempts;

        // act
        try
        {
            start = CreateServices(vhost, recorder, connectionProvider, Timeout.InfiniteTimeSpan).BuildTestBusAsync();

            // the two connections fail six attempts between them while the broker is stopped
            await Wait.UntilAsync(() => connectionProvider.FailedAttempts >= 6 || start.IsCompleted);
            pendingAfterFailedAttempts = !start.IsCompleted;
        }
        finally
        {
            await broker.DisposeAsync();
        }

        await using var bus = await start.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        Assert.True(pendingAfterFailedAttempts);
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_BrokerRefusesOneOfTheTwoConnections()
    {
        // arrange
        // the broker completes the handshake of a connection above the limit and refuses it afterwards. It
        // counts a connection with a delay, so the second connection waits until the first one is counted
        await using var vhost = await fixture.CreateVhostAsync();
        await using var limit = await vhost.Faults.LimitConnectionsAsync(1);
        var recorder = new MessageRecorder();
        var connectionProvider = new SequentialConnectionProvider(vhost);

        // act
        var exception = await Assert.ThrowsAnyAsync<OperationInterruptedException>(
            () => StartAndDisposeAsync(CreateServices(vhost, recorder, connectionProvider)));

        // assert
        Assert.Equal<ushort?>(Constants.NotAllowed, exception.ShutdownReason?.ReplyCode);
        await vhost.Broker.WaitForNoConnectionAsync();
    }

    [Fact]
    public async Task Connections_Should_BeNamedByRole_When_Started()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);

        // act
        var names = await vhost.Broker.GetConnectionNamesAsync();

        // assert
        Assert.Equal(["consumer", "dispatcher"], names.Select(name => name[(name.LastIndexOf(' ') + 1)..]));
    }

    [Fact]
    public async Task StartAsync_Should_Connect_When_FirstNodeUnreachable()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var connectionProvider = new ClusterConnectionProvider(vhost);

        // act
        await using var bus = await CreateBusAsync(vhost, recorder, connectionProvider);

        // assert
        Assert.Equal(
            [
                "mocha-unreachable.invalid", vhost.ConnectionSettings.HostName,
                "mocha-unreachable.invalid", vhost.ConnectionSettings.HostName
            ],
            connectionProvider.ConnectionAttempts.SelectMany(attempt => attempt));
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
    }

    private static Task<TestBus> CreateBusAsync(
        VhostContext vhost,
        MessageRecorder recorder,
        IRabbitMQConnectionProvider? connectionProvider = null)
        => CreateServices(vhost, recorder, connectionProvider).BuildTestBusAsync();

    private static IMessageBusHostBuilder CreateServices(
        VhostContext vhost,
        MessageRecorder recorder,
        IRabbitMQConnectionProvider? connectionProvider = null,
        TimeSpan? startTimeout = null)
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

                if (startTimeout is { } timeout)
                {
                    t.StartTimeout(timeout);
                }

                t.Endpoint("orders").Handler<OrderCreatedHandler>();
                t.Endpoint("order-status").Handler<GetOrderStatusHandler>();
            });

    private static async Task StartAndDisposeAsync(IMessageBusHostBuilder builder)
    {
        await using var services = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken)
            .AsTask().WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
    }

    private static async Task AssertRoundTripAsync(TestBus bus, MessageRecorder recorder, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CreateTimeout();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = orderId }, deadline.Token);
        Assert.True(await Condition.WaitAsync(
            () => recorder.Messages.Cast<OrderCreated>().Any(order => order.OrderId == orderId),
            TestTimeouts.Signal));
        var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = orderId }, deadline.Token);
        Assert.Equal(orderId, response.OrderId);
        Assert.Equal("Shipped", response.Status);
    }

    private static CancellationTokenSource CreateTimeout()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TestTimeouts.Signal);
        return cts;
    }

    private sealed class ClusterConnectionProvider(VhostContext vhost) : IRabbitMQConnectionProvider
    {
        public ConcurrentQueue<string[]> ConnectionAttempts { get; } = new();

        public string Host => vhost.ConnectionSettings.HostName;

        public string VirtualHost => vhost.VhostName;

        public int Port => vhost.ConnectionSettings.Port;

        public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory { Uri = vhost.ConnectionSettings.Uri, VirtualHost = vhost.VhostName };
            AmqpTcpEndpoint[] nodes =
            [
                new("mocha-unreachable.invalid", vhost.ConnectionSettings.Port),
                new(vhost.ConnectionSettings.HostName, vhost.ConnectionSettings.Port)
            ];

            var resolver = new OrderedEndpointResolver(nodes);
            var connection = await factory.CreateConnectionAsync(resolver, null, cancellationToken);
            ConnectionAttempts.Enqueue(resolver.AttemptedHosts.ToArray());
            return connection;
        }
    }

    private sealed class CountingConnectionProvider(VhostContext vhost) : IRabbitMQConnectionProvider
    {
        private int _failedAttempts;

        public int FailedAttempts => Volatile.Read(ref _failedAttempts);

        public string Host => vhost.ConnectionSettings.HostName;

        public string VirtualHost => vhost.VhostName;

        public int Port => vhost.ConnectionSettings.Port;

        public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await vhost.ConnectionFactory.CreateConnectionAsync(cancellationToken);
            }
            catch
            {
                Interlocked.Increment(ref _failedAttempts);
                throw;
            }
        }
    }

    private sealed class SequentialConnectionProvider(VhostContext vhost) : IRabbitMQConnectionProvider
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private int _connections;

        public string Host => vhost.ConnectionSettings.HostName;

        public string VirtualHost => vhost.VhostName;

        public int Port => vhost.ConnectionSettings.Port;

        public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                await vhost.Broker.WaitForCountedConnectionsAsync(_connections);
                var connection = await vhost.ConnectionFactory.CreateConnectionAsync(cancellationToken);
                _connections++;
                return connection;
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private sealed class OrderedEndpointResolver(AmqpTcpEndpoint[] endpoints) : IEndpointResolver
    {
        public List<string> AttemptedHosts { get; } = [];

        public IEnumerable<AmqpTcpEndpoint> All()
        {
            foreach (var endpoint in endpoints)
            {
                AttemptedHosts.Add(endpoint.HostName);
                yield return endpoint;
            }
        }
    }
}
