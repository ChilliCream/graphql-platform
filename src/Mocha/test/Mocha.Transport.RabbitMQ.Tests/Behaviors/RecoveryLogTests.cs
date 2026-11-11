using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// Pins what an operator reads at Information level and above for each incident.
/// </summary>
[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class RecoveryLogTests(RabbitMQFixture fixture)
{
    private const string Template = "{OriginalFormat}";

    [Fact]
    public async Task Log_Should_ReportLossAttemptsAndRecoveryPerConnection_When_BrokerRestarts()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, logs, orders);
        await PublishAsync(bus, "ORD-1");
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));
        var before = await vhost.Broker.GetConsumerAsync("orders");
        logs.Clear();

        // act
        await using (await fixture.Faults.StopBrokerAsync())
        {
            await WaitForFailedAttemptOfBothConnectionsAsync(logs);
        }

        await vhost.Broker.WaitForConsumerAsync("orders", otherThan: before);
        await Wait.UntilAsync(() => bus.Transport.HealthFailureReason is null);
        var entries = logs.GetTransportEntries();

        // assert
        Assert.Equal(
            [
                "Warning ConnectionLost",
                "Warning ConnectionAttemptFailed",
                "Information ConnectionReady",
                "Information ConsumerAttached"
            ],
            Summarize(entries, "consumer"));
        Assert.Equal(
            ["Warning ConnectionLost", "Warning ConnectionAttemptFailed", "Information ConnectionReady"],
            Summarize(entries, "dispatcher"));
    }

    [Fact]
    public async Task Log_Should_ReportOneErrorPerAttemptAndConnection_When_AccessIsRevoked()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, logs, orders);
        var before = await vhost.Broker.GetConsumerAsync("orders");
        logs.Clear();

        // act
        await using (await vhost.Faults.RevokeAccessAsync())
        {
            await vhost.Faults.CloseConnectionsAsync();
            await WaitForFailedAttemptOfBothConnectionsAsync(logs);
        }

        await vhost.Broker.WaitForConsumerAsync("orders", otherThan: before);
        await Wait.UntilAsync(() => bus.Transport.HealthFailureReason is null);
        var entries = logs.GetTransportEntries();

        // assert
        Assert.Equal(
            [
                "Warning ConnectionLost",
                "Error ConnectionAttemptFailed",
                "Information ConnectionReady",
                "Information ConsumerAttached"
            ],
            Summarize(entries, "consumer"));
        Assert.Equal(
            ["Warning ConnectionLost", "Error ConnectionAttemptFailed", "Information ConnectionReady"],
            Summarize(entries, "dispatcher"));
        Assert.Equal(
            "RabbitMQ connection {ConnectionName} is unavailable for {Elapsed}: attempt {Attempt} failed. "
            + "Retrying in {Delay}",
            logs.GetEntries("ConnectionAttemptFailed")[0].Properties[Template]);
    }

    [Fact]
    public async Task Log_Should_NameTheMissedHeartbeat_When_ConnectionStopsAnswering()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedHeartbeat = TimeSpan.FromSeconds(2);
        var logs = new LogRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, logs, new MessageRecorder());
        logs.Clear();
        LogEntry lost;

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            lost = await logs.WaitForAsync("ConnectionLost");
        }

        // assert
        Assert.Equal(LogLevel.Warning, lost.Level);
        Assert.Equal("Heartbeat missing with heartbeat == 00:00:02 seconds", lost.Properties["Cause"]);
        Assert.Equal(
            "RabbitMQ connection {ConnectionName} was lost: {ReplyCode} {ReplyText} (initiator: {Initiator}, "
            + "cause: {Cause}). Reconnecting in the background",
            lost.Properties[Template]);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(4, 1)]
    public async Task Log_Should_CountCancelledHandlersWithoutAnError_When_HandlersObserveCancellation(
        int maxConcurrency,
        int runningHandlers)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        var gate = new HandlerGate();
        await using var bus = await CreateServices(vhost, logs)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<CooperativeOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<CooperativeOrderHandler>().MaxConcurrency(maxConcurrency))
            .BuildTestBusAsync();
        for (var i = 0; i < runningHandlers; i++)
        {
            await PublishAsync(bus, $"ORD-{i}");
        }

        await Wait.UntilAsync(() => gate.StartedHandlers == runningHandlers);
        var before = await vhost.Broker.GetConsumerAsync("orders");
        logs.Clear();

        // act
        await vhost.Faults.CloseConsumerConnectionAsync("orders");
        await vhost.Broker.WaitForConsumerAsync("orders", otherThan: before);
        gate.Release.TrySetResult();
        var lost = Assert.Single(logs.GetEntries("ConsumerChannelLost"), entry => IsAbout(entry, "orders"));

        // assert
        Assert.Empty(logs.GetTransportEntries(LogLevel.Error));
        Assert.Equal(LogLevel.Warning, lost.Level);
        Assert.Equal(runningHandlers, lost.Properties["CancelledHandlers"]);
        Assert.Equal(
            "The consumer for queue {Queue} on {ConnectionName} lost its channel: {ReplyCode} {ReplyText} "
            + "(initiator: {Initiator}). {CancelledHandlers} running handlers were cancelled, and the broker "
            + "delivers their messages again",
            lost.Properties[Template]);
    }

    [Fact]
    public async Task Log_Should_ReportBlockedConsumerAsError_When_HandlersIgnoreCancellationPastTheGracePeriod()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        var gate = new HandlerGate();
        await using var bus = await CreateStuckOrdersBusAsync(vhost, logs, gate);
        bus.Transport.ConsumerManager.BlockedGracePeriod = TimeSpan.FromSeconds(1);
        await PublishAsync(bus, "stuck");
        await Wait.UntilAsync(() => gate.StartedHandlers == 1);
        logs.Clear();

        try
        {
            // act
            await vhost.Faults.DeleteQueueAsync("orders");
            var blocked = await logs.WaitForAsync("ConsumerBlockedByHandlers");
            gate.Release.TrySetResult();
            await vhost.Broker.WaitForConsumerAsync("orders");
            await Wait.UntilAsync(() => bus.Transport.HealthFailureReason is null);

            // assert
            Assert.Equal(
                [
                    "Warning ConsumerCancelled",
                    "Error ConsumerBlockedByHandlers",
                    "Information ConsumerUnblocked",
                    "Information ConsumerAttached"
                ],
                Summarize(logs.GetTransportEntries().Where(entry => IsAbout(entry, "orders")), "consumer"));
            Assert.Equal(
                "The consumer for queue {Queue} on {ConnectionName} cannot attach: all {MaxConcurrency} handlers "
                + "of a lost channel are still running {GracePeriod} after they were cancelled. The endpoint "
                + "consumes again when one of them returns",
                blocked.Properties[Template]);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Log_Should_ReportDrain_When_BusStopsWhileAHandlerRuns()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        var gate = new HandlerGate();
        await using var bus = await CreateStuckOrdersBusAsync(vhost, logs, gate);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        await PublishAsync(bus, "stuck");
        await Wait.UntilAsync(() => gate.StartedHandlers == 1);
        logs.Clear();

        // act
        var stop = runtime.StopAsync(TestContext.Current.CancellationToken).AsTask();
        await logs.WaitForAsync("ConsumerDrainStarted");
        gate.Release.TrySetResult();
        await stop.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        var entries = logs.GetTransportEntries();

        // assert
        Assert.Equal(
            ["Information ConsumerDrainStarted", "Information ConsumerStopped"],
            Summarize(entries.Where(entry => IsAbout(entry, "orders")), "consumer"));
        Assert.Equal(
            ["Information ConsumerStopped"],
            Summarize(entries.Where(entry => !IsAbout(entry, "orders")), "consumer"));
    }

    [Fact]
    public async Task Log_Should_SayThatHandlersWereCancelled_When_StopDeadlineElapses()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        var gate = new HandlerGate();
        await using var bus = await CreateStuckOrdersBusAsync(vhost, logs, gate);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        await PublishAsync(bus, "stuck");
        await Wait.UntilAsync(() => gate.StartedHandlers == 1);
        logs.Clear();

        try
        {
            // act
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await runtime.StopAsync(deadline.Token).AsTask()
                .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
            var entries = logs.GetTransportEntries();

            // assert
            Assert.Equal(
                [
                    "Information ConsumerDrainStarted",
                    "Warning ConsumerDrainTimedOut",
                    "Information ConsumerStopped"
                ],
                Summarize(entries.Where(entry => IsAbout(entry, "orders")), "consumer"));
            Assert.Equal(
                ["Information ConsumerStopped"],
                Summarize(entries.Where(entry => !IsAbout(entry, "orders")), "consumer"));
            Assert.Equal(
                "The consumer for queue {Queue} on {ConnectionName} stopped waiting for its handlers after "
                + "{Elapsed}. {RunningHandlers} handlers were cancelled, and the broker returns their "
                + "unacknowledged messages to the queue",
                logs.GetEntries("ConsumerDrainTimedOut")[0].Properties[Template]);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Log_Should_ReportReturnAndRepair_When_QueueOfSentMessageIsMissing()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        await using var bus = await CreatePaymentsBusAsync(vhost, logs);
        await SendAsync(bus, "warmup");
        await vhost.Faults.DeleteQueueAsync("payments");
        logs.Clear();

        // act
        await SendAsync(bus, "ORD-1");
        var entries = logs.GetTransportEntries();

        // assert
        Assert.Equal(["Warning MessageReturned", "Information QueueRedeclared"], Summarize(entries, "dispatcher"));
        Assert.Equal(
            [
                "The broker returned message {MessageId} because queue {Queue} does not exist on "
                + "{ConnectionName}. Declaring the queue and its bindings again",
                "Declared queue {Queue} and its bindings again on {ConnectionName}. Publishing message "
                + "{MessageId} again"
            ],
            entries.Select(entry => entry.Properties[Template]));
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    [Fact]
    public async Task Log_Should_ReportStalledPublishesInOneLine_When_BrokerBlocksThePublisher()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        await using var bus = await CreatePaymentsBusAsync(vhost, logs);
        await SendAsync(bus, "warmup");
        bus.Transport.Dispatcher.StalledPublishInterval = TimeSpan.FromSeconds(1);
        logs.Clear();
        Task[] sends;
        LogEntry stalled;

        // act
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            sends = [SendAsync(bus, "ORD-1"), SendAsync(bus, "ORD-2"), SendAsync(bus, "ORD-3")];
            stalled = (await Wait.UntilAsync(
                () => Task.FromResult(
                    logs.GetEntries("PublishesStalled").FirstOrDefault(entry => Equals(entry.Properties["Count"], 3L))),
                entry => entry is not null,
                "one report about three stalled publishes"))!;
        }

        await Task.WhenAll(sends).WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(LogLevel.Warning, stalled.Level);
        Assert.Equal("blocked by the broker", stalled.Properties["ConnectionStatus"]);
        Assert.Equal(
            "{Count} publishes on RabbitMQ connection {ConnectionName} have waited for the broker for at least "
            + "{Interval}. The connection is {ConnectionStatus}",
            stalled.Properties[Template]);
    }

    [Fact]
    public async Task Log_Should_ExplainPossibleDuplicate_When_PublishIsAttemptedAgain()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var logs = new LogRecorder();
        await using var bus = await CreatePaymentsBusAsync(vhost, logs);
        await SendAsync(bus, "warmup");
        logs.Clear();
        Task send;
        LogEntry retry;

        // act
        // the broker keeps the publish unconfirmed, then its connection is closed under it
        await using (await fixture.Faults.RaiseMemoryAlarmAsync())
        {
            send = SendAsync(bus, "ORD-1");
            await vhost.Broker.WaitForBlockedPublisherAsync();
            await vhost.Faults.CloseConnectionsAsync();
            retry = await logs.WaitForAsync("PublishRetryScheduled");
        }

        await send.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(LogLevel.Information, retry.Level);
        Assert.Equal(1, retry.Properties["Attempt"]);
        Assert.Equal(
            "Publishing message {MessageId} to endpoint {Endpoint} on {ConnectionName} failed (attempt {Attempt}) "
            + "and is attempted again in {Delay}. The broker delivers the message twice if it already has it. "
            + "Reason: {Reason}",
            retry.Properties[Template]);
    }

    private static string[] Summarize(IEnumerable<LogEntry> entries, string connectionRole)
        =>
        [
            .. entries
                .Where(entry => Equals(entry.Properties.GetValueOrDefault("ConnectionRole"), connectionRole))
                .Select(entry => $"{entry.Level} {entry.EventName}")
                .Distinct()
        ];

    private static bool IsAbout(LogEntry entry, string queue)
        => Equals(entry.Properties.GetValueOrDefault("Queue"), queue);

    private static Task WaitForFailedAttemptOfBothConnectionsAsync(LogRecorder logs)
        => Wait.UntilAsync(
            () => logs.GetEntries("ConnectionAttemptFailed")
                .Select(entry => entry.Properties.GetValueOrDefault("ConnectionRole"))
                .Distinct()
                .Count() == 2);

    private static IServiceCollection CreateServices(VhostContext vhost, LogRecorder logs)
        => new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(logs))
            .AddSingleton(vhost.ConnectionFactory);

    private static Task<TestBus> CreateOrdersBusAsync(VhostContext vhost, LogRecorder logs, MessageRecorder orders)
        => CreateServices(vhost, logs)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();

    private static Task<TestBus> CreateStuckOrdersBusAsync(VhostContext vhost, LogRecorder logs, HandlerGate gate)
        => CreateServices(vhost, logs)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<StuckOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<StuckOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();

    private static Task<TestBus> CreatePaymentsBusAsync(VhostContext vhost, LogRecorder logs)
        => CreateServices(vhost, logs)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("payments");
                t.DispatchEndpoint("payment-send").ToQueue("payments").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new OrderCreated { OrderId = orderId }, deadline.Token);
    }

    private static async Task SendAsync(TestBus bus, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .SendAsync(new ProcessPayment { OrderId = orderId, Amount = 1 }, deadline.Token);
    }

    public sealed class HandlerGate
    {
        private int _startedHandlers;

        public int StartedHandlers => Volatile.Read(ref _startedHandlers);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnStarted() => Interlocked.Increment(ref _startedHandlers);
    }

    /// <summary>
    /// Runs until it is released or cancelled.
    /// </summary>
    public sealed class CooperativeOrderHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.OnStarted();
            await gate.Release.Task.WaitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Runs until it is released, and ignores its cancellation.
    /// </summary>
    public sealed class StuckOrderHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.OnStarted();
            await gate.Release.Task;
        }
    }
}
