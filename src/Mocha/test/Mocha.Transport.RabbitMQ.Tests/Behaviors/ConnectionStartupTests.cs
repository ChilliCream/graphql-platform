using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class ConnectionStartupTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_recoveryTimeout = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task StartAsync_Should_Complete_When_BrokerBecomesReachableDuringStart()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var broker = await fixture.StopBrokerAsync();
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

        await using var bus = await start.WaitAsync(s_recoveryTimeout, TestContext.Current.CancellationToken);

        // assert
        await AssertRoundTripAsync(bus, recorder, "ORD-1");
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_VirtualHostAccessRefused()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var revoked = await vhost.RevokeAccessAsync();

        // act
        var exception = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => StartAndDisposeAsync(CreateServices(vhost, recorder)));

        // assert
        Assert.Equal<ushort?>(Constants.NotAllowed, exception.ShutdownReason?.ReplyCode);
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_CredentialsRefused()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.Password = "wrong-password";
        var recorder = new MessageRecorder();

        // act
        var exception = await Assert.ThrowsAsync<BrokerUnreachableException>(
            () => StartAndDisposeAsync(CreateServices(vhost, recorder)));

        // assert
        Assert.IsType<AuthenticationFailureException>(exception.InnerException);
        Assert.Equal(0, await vhost.GetConnectionCountAsync());
    }

    [Fact]
    public async Task StartAsync_Should_FailFastWithoutLeakingConnections_When_ConfigurePermissionDenied()
    {
        // arrange
        // the user may open the vhost and publish, but declares nothing
        await using var vhost = await fixture.CreateVhostAsync();
        await vhost.SetPermissionsAsync("guest", configure: "^$", write: ".*", read: ".*");
        var recorder = new MessageRecorder();

        // act
        var exception = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => StartAndDisposeAsync(CreateServices(vhost, recorder)));

        // assert
        Assert.Equal<ushort?>(Constants.AccessRefused, exception.ShutdownReason?.ReplyCode);
        Assert.True(await Condition.WaitAsync(async () => await vhost.GetConnectionCountAsync() == 0, s_timeout));
    }

    [Fact]
    public async Task StartAsync_Should_Fail_When_BrokerUnreachableForConnectTimeout()
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
                t.ConnectTimeout(TimeSpan.FromSeconds(3));
                t.Endpoint("orders").Handler<OrderCreatedHandler>();
            })
            .Services
            .BuildServiceProvider();
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();

        // act
        await Assert.ThrowsAsync<BrokerUnreachableException>(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken));

        // assert
        Assert.False(runtime.IsStarted);
        Assert.Equal(0, await vhost.GetConnectionCountAsync());
    }

    [Fact]
    public async Task Connections_Should_BeNamedByRole_When_Started()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);

        // act
        var names = await vhost.GetConnectionNamesAsync();

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

    private static async Task StartAndDisposeAsync(IMessageBusHostBuilder builder)
    {
        await using var services = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken)
            .AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);
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

    private static CancellationTokenSource CreateTimeout()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
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
