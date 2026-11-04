using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class ConnectionStateTests(RabbitMQFixture fixture, ITestOutputHelper output)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_recoveryTimeout = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task RequestAsync_Should_Succeed_When_BrokerClosesConsumerConnection()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        var before = await WarmUpAsync(vhost, bus, recorder);

        // act
        // the reply queue is auto-delete, so the broker deletes it once its only consumer is gone
        await vhost.CloseConsumerConnectionsAsync();
        var outcome = await RecoverAsync(vhost, bus, recorder, before);
        var after = await vhost.WaitForStateAsync(before, s_timeout);
        output.WriteLine($"before: {before}, after: {after}");

        // assert
        Assert.True(after.Matches(before));
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task Bus_Should_ResumeWithoutLeaks_When_BrokerRestarts()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs);
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        var broker = await fixture.StopBrokerAsync();
        await broker.DisposeAsync();
        var outcome = await RecoverAsync(vhost, bus, recorder, before);
        var after = await vhost.WaitForStateAsync(before, s_timeout);
        output.WriteLine($"before: {before}, after: {after}");

        // assert
        Assert.True(after.Matches(before));
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task Bus_Should_ResumeWithoutLeaks_When_HeartbeatsMissed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedHeartbeat = TimeSpan.FromSeconds(1);
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs);
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        // the broker stops sending heartbeats, so the client gives up on its connections after three intervals
        bool detected;
        await using (await vhost.FreezeConnectionsAsync())
        {
            detected = await Condition.WaitAsync(() => logs.Contains("ConnectionShutdownDetected"), s_timeout);
        }

        var outcome = await RecoverAsync(vhost, bus, recorder, before);
        var after = await vhost.WaitForStateAsync(before, s_timeout);
        output.WriteLine($"before: {before}, after: {after}");

        // assert
        Assert.True(detected);
        Assert.True(after.Matches(before));
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task StartAsync_Should_Complete_When_BrokerBecomesReachableDuringStart()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        var broker = await fixture.StopBrokerAsync();
        Task<TestBus> start;
        bool completedWhileBrokerStopped;

        // act
        try
        {
            start = CreateBusAsync(vhost, recorder, logs);
            Assert.True(await Condition.WaitAsync(() => logs.Contains("FailedToCreateConnectionRetrying"), s_timeout));
            completedWhileBrokerStopped = start.IsCompleted;
        }
        finally
        {
            await broker.DisposeAsync();
        }

        await using var bus = await start.WaitAsync(s_recoveryTimeout, TestContext.Current.CancellationToken);
        var outcome = await RoundTripAsync(bus, recorder, "ORD-1");

        // assert
        Assert.False(completedWhileBrokerStopped);
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_VirtualHostAccessRefused()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var revoked = await vhost.RevokeAccessAsync();

        // act
        string start;
        try
        {
            await using var bus = await CreateBusAsync(vhost, recorder)
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            start = "started";
        }
        catch (Exception ex)
        {
            start = ex.GetType().Name;
        }

        // assert
        Assert.Equal("OperationInterruptedException", start);
    }

    [Fact]
    public async Task DisposeAsync_Should_LeaveNoConnection_When_BrokerUnreachableDuringRecovery()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        var bus = await CreateBusAsync(vhost, recorder, logs);
        var transport = GetTransport(bus);
        await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        var disposed = false;
        await using (await fixture.StopBrokerAsync())
        {
            await Condition.WaitAsync(() => !transport.ConsumerManager.IsConnected, s_timeout);
            await bus.DisposeAsync().AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            disposed = true;
        }

        // a recovery that outlived the dispose would reconnect within a few recovery intervals
        var reconnected = await Condition.WaitAsync(
            async () => await vhost.GetConnectionCountAsync() > 0,
            TimeSpan.FromSeconds(5));

        // assert
        Assert.True(disposed);
        Assert.False(reconnected);
    }

    private static Task<TestBus> CreateBusAsync(
        VhostContext vhost,
        MessageRecorder recorder,
        LogRecorder? logs = null,
        IRabbitMQConnectionProvider? connectionProvider = null)
        => CreateServices(vhost, recorder, logs, connectionProvider).BuildTestBusAsync();

    private static IMessageBusHostBuilder CreateServices(
        VhostContext vhost,
        MessageRecorder recorder,
        LogRecorder? logs = null,
        IRabbitMQConnectionProvider? connectionProvider = null)
        => new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs ?? new LogRecorder()))
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

    [Fact]
    public async Task RequestAsync_Should_FailWithinTimeout_When_BrokerStopped()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        await WarmUpAsync(vhost, bus, recorder);

        // act
        var outcomes = new List<string>();
        await using (await fixture.StopBrokerAsync())
        {
            for (var i = 0; i < 3; i++)
            {
                using var scope = bus.Provider.CreateScope();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    await messageBus.RequestAsync(new GetOrderStatus { OrderId = $"ORD-{i}" }, cts.Token);
                    outcomes.Add("replied");
                }
                catch (OperationCanceledException)
                {
                    outcomes.Add(stopwatch.Elapsed < TimeSpan.FromSeconds(4) ? "cancelled in time" : "cancelled late");
                }
                catch (Exception ex)
                {
                    outcomes.Add(ex.GetType().Name);
                }
            }
        }

        // assert
        Assert.Equal(["cancelled in time", "cancelled in time", "cancelled in time"], outcomes);
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_CredentialsRefused()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.Password = "wrong-password";
        var recorder = new MessageRecorder();

        // act
        var stopwatch = Stopwatch.StartNew();
        var exception = await StartAndDisposeAsync(CreateServices(vhost, recorder));
        var elapsed = stopwatch.Elapsed;
        var connections = await vhost.GetConnectionCountAsync();

        // assert
        Assert.NotNull(FindException<AuthenticationFailureException>(exception));
        Assert.True(elapsed < s_timeout);
        Assert.Equal(0, connections);
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
        var stopwatch = Stopwatch.StartNew();
        var exception = await StartAndDisposeAsync(CreateServices(vhost, recorder));
        var elapsed = stopwatch.Elapsed;
        var closed = await Condition.WaitAsync(
            async () => await vhost.GetConnectionCountAsync() == 0,
            TimeSpan.FromSeconds(5));

        // assert
        Assert.Equal<ushort?>(403, FindException<OperationInterruptedException>(exception)?.ShutdownReason?.ReplyCode);
        Assert.True(elapsed < s_timeout);
        Assert.True(closed);
    }

    [Fact]
    public async Task StartAsync_Should_Fail_When_BrokerUnreachableForConnectTimeout()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.HostName = "mocha-unreachable.invalid";
        var logs = new LogRecorder();
        await using var services = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
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
        var exception = await Record.ExceptionAsync(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken));

        // assert
        Assert.NotNull(FindException<BrokerUnreachableException>(exception));
        Assert.True(logs.Contains("FailedToCreateConnectionRetrying"));
    }

    [Fact]
    public async Task Bus_Should_Reconnect_When_PasswordRotated()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-rotating", "first-password");
        await using var user = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs, new UserConnectionProvider(vhost, credentials));
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        // open connections keep the old password, and the provider supplies the new one for every new connection
        await vhost.ChangePasswordAsync(credentials.UserName, "second-password");
        credentials.Password = "second-password";
        await fixture.CloseAllConnectionsAsync();
        var outcome = await RecoverAsync(vhost, bus, recorder, before);
        var after = await vhost.WaitForStateAsync(before, s_timeout);

        // assert
        Assert.True(after.Matches(before));
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task Bus_Should_Reconnect_When_UserDeletedAndCreatedAgain()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-deleted", "password");
        await using var user = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs, new UserConnectionProvider(vhost, credentials));
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        // deleting the user closes its connections, and reconnects fail with an authentication error until the
        // user exists again
        await vhost.DeleteUserAsync(credentials.UserName);
        var lost = await Condition.WaitAsync(() => logs.Contains("ConnectionShutdownDetected"), s_timeout);
        Assert.True(await Condition.WaitAsync(() => logs.Contains("ReconnectionAttemptFailed"), s_timeout));
        await using var recreated = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        var outcome = await RecoverAsync(vhost, bus, recorder, before);
        var after = await vhost.WaitForStateAsync(before, s_timeout);

        // assert
        Assert.True(lost);
        Assert.True(after.Matches(before));
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task Bus_Should_ResumeWithoutLeaks_When_BrokerRestartedRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs);
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        var outcomes = new List<RoundTrip>();
        for (var i = 0; i < 3; i++)
        {
            var broker = await fixture.StopBrokerAsync();
            await broker.DisposeAsync();
            outcomes.Add(await RecoverAsync(vhost, bus, recorder, before));
        }

        var after = await vhost.WaitForStateAsync(before, s_timeout);

        // assert
        Assert.True(after.Matches(before));
        Assert.Equal(
            [
                new RoundTrip("received", "Shipped"),
                new RoundTrip("received", "Shipped"),
                new RoundTrip("received", "Shipped")
            ],
            outcomes);
    }

    [Fact]
    public async Task Bus_Should_Resume_When_NodeDrainedAndRevived()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs);
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        // maintenance mode closes every client connection with 320 and refuses new ones until the node is revived
        bool closed;
        await using (await fixture.DrainNodeAsync())
        {
            closed = await Condition.WaitAsync(
                async () => await vhost.GetConnectionCountAsync() == 0,
                s_timeout);
        }

        var outcome = await RecoverAsync(vhost, bus, recorder, before);
        var after = await vhost.WaitForStateAsync(before, s_timeout);

        // assert
        Assert.True(closed);
        Assert.True(after.Matches(before));
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    [Fact]
    public async Task Bus_Should_KeepBrokerResourcesFlat_When_ConsumerConnectionClosedRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder, logs);
        var before = await WarmUpAsync(vhost, bus, recorder);
        using var dump = new Dump(output, logs);

        // act
        var recovered = new List<bool>();
        for (var i = 0; i < 5; i++)
        {
            await vhost.CloseConsumerConnectionsAsync();
            var outcome = await RecoverAsync(vhost, bus, recorder, before);
            var state = await vhost.WaitForStateAsync(before, s_timeout);
            recovered.Add(outcome == new RoundTrip("received", "Shipped") && state.Matches(before));
        }

        // assert
        Assert.Equal([true, true, true, true, true], recovered);
    }

    [Fact]
    public async Task Bus_Should_RaiseNoUnobservedTaskException_When_FaultsInjected()
    {
        // arrange
        var unobserved = new ConcurrentQueue<Exception>();
        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // other tests of the process can raise the event too
            if (e.Exception.ToString().Contains("Mocha.Transport.RabbitMQ", StringComparison.Ordinal))
            {
                unobserved.Enqueue(e.Exception);
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            await using var vhost = await fixture.CreateVhostAsync();
            vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
            var recorder = new MessageRecorder();
            var bus = await CreateBusAsync(vhost, recorder);
            var before = await WarmUpAsync(vhost, bus, recorder);

            // act
            await vhost.CloseConsumerConnectionsAsync();
            await RecoverAsync(vhost, bus, recorder, before);
            var broker = await fixture.StopBrokerAsync();
            await broker.DisposeAsync();
            await RecoverAsync(vhost, bus, recorder, before);
            await bus.DisposeAsync();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);

            // assert
            Assert.Empty(unobserved);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    public async Task RequestAsync_Should_ReceiveReply_When_RequesterConsumerConnectionDropsBeforeReply()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var credentials = new Credentials($"{vhost.VhostName}-requester", "password");
        await using var user = await vhost.AddUserAsync(credentials.UserName, credentials.Password);
        var gate = new ReplyGate();
        await using var responder = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddRequestHandler<GatedOrderStatusHandler>()
            .AddRabbitMQ(t => t.Endpoint("order-status").Handler<GatedOrderStatusHandler>())
            .BuildTestBusAsync();
        await using var requester = await new ServiceCollection()
            .AddMessageBus()
            .AddRabbitMQ(t => t.ConnectionProvider(_ => new UserConnectionProvider(vhost, credentials)))
            .BuildTestBusAsync();
        var replyQueue = ((RabbitMQReceiveEndpoint)GetTransport(requester).ReplyReceiveEndpoint!).Queue.Name;

        // act
        using var scope = requester.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        var request = scope.ServiceProvider.GetRequiredService<IMessageBus>().RequestAsync(
            new GetOrderStatus { OrderId = "ORD-1" },
            new SendOptions { Endpoint = new Uri("rabbitmq:q/order-status") },
            cts.Token).AsTask();
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // the requester cannot attach again until its permissions are restored, so the reply is sent while its
        // reply queue has no consumer
        await vhost.SetPermissionsAsync(credentials.UserName, configure: "^$", write: "^$", read: "^$");
        await vhost.CloseConnectionOfConsumerAsync(replyQueue);
        await vhost.WaitForNoConsumerAsync(replyQueue, s_timeout);
        gate.Release.TrySetResult();
        await vhost.WaitForMessagesAsync(replyQueue, 1, s_timeout);
        await vhost.SetPermissionsAsync(credentials.UserName, configure: ".*", write: ".*", read: ".*");

        string reply;
        try
        {
            reply = (await request).Status;
        }
        catch (Exception ex)
        {
            reply = ex.GetType().Name;
        }

        // assert
        Assert.Equal("Shipped", reply);
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

        // act
        await using var bus = await CreateBusAsync(vhost, recorder, connectionProvider: new ClusterConnectionProvider(vhost));
        var outcome = await RoundTripAsync(bus, recorder, "ORD-1");

        // assert
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
    }

    private static async Task<Exception?> StartAndDisposeAsync(IMessageBusHostBuilder builder)
    {
        var services = builder.Services.BuildServiceProvider();
        try
        {
            var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();
            return await Record.ExceptionAsync(
                () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask()
                    .WaitAsync(s_timeout, TestContext.Current.CancellationToken));
        }
        finally
        {
            await services.DisposeAsync();
        }
    }

    private static T? FindException<T>(Exception? exception) where T : Exception
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is T match)
            {
                return match;
            }

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (FindException<T>(inner) is { } nested)
                    {
                        return nested;
                    }
                }
            }
        }

        return null;
    }

    private static RabbitMQMessagingTransport GetTransport(TestBus bus)
        => bus.Provider
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>()
            .Single();

    private static async Task<RoundTrip> RecoverAsync(
        VhostContext vhost,
        TestBus bus,
        MessageRecorder recorder,
        BrokerState before)
    {
        // pooled publish channels come back with the next publish, so only the consumers are awaited first
        await Condition.WaitAsync(
            async () => await vhost.GetStateAsync() is var state
                && state.Consumers == before.Consumers
                && state.ConsumedQueues == before.ConsumedQueues,
            s_recoveryTimeout);

        return await RoundTripAsync(bus, recorder, "ORD-2");
    }

    private static async Task<BrokerState> WarmUpAsync(VhostContext vhost, TestBus bus, MessageRecorder recorder)
    {
        var outcome = await RoundTripAsync(bus, recorder, "ORD-1");
        Assert.Equal(new RoundTrip("received", "Shipped"), outcome);
        return await vhost.GetStateAsync();
    }

    private static async Task<RoundTrip> RoundTripAsync(TestBus bus, MessageRecorder recorder, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        string received;
        try
        {
            using var cts = CreateTimeout();
            await messageBus.PublishAsync(new OrderCreated { OrderId = orderId }, cts.Token);
            received = await recorder.WaitAsync(s_timeout) ? "received" : "not received";
        }
        catch (Exception ex)
        {
            received = ex.GetType().Name;
        }

        string reply;
        try
        {
            using var cts = CreateTimeout();
            var response = await messageBus.RequestAsync(new GetOrderStatus { OrderId = orderId }, cts.Token);
            reply = response.Status;
        }
        catch (Exception ex)
        {
            reply = ex.GetType().Name;
        }

        return new RoundTrip(received, reply);
    }

    private static CancellationTokenSource CreateTimeout()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
        return cts;
    }

    private sealed record RoundTrip(string Event, string Reply);

    private sealed class Dump(ITestOutputHelper output, LogRecorder logs) : IDisposable
    {
        public void Dispose() => logs.WriteTo(output);
    }

    private sealed class Credentials(string userName, string password)
    {
        public string UserName => userName;

        public string Password { get; set; } = password;
    }

    /// <summary>
    /// Connects as a dedicated user, and reads the password anew for every connection.
    /// </summary>
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

    /// <summary>
    /// Connects to the first reachable node of a list whose first node does not exist.
    /// </summary>
    private sealed class ClusterConnectionProvider(VhostContext vhost) : IRabbitMQConnectionProvider
    {
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

            return await factory.CreateConnectionAsync(nodes, cancellationToken);
        }
    }

    public sealed class ReplyGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class GatedOrderStatusHandler(ReplyGate gate)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public async ValueTask<OrderStatusResponse> HandleAsync(
            GetOrderStatus request,
            CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            return new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" };
        }
    }
}
