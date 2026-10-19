using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
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
        new { Recovered = after.Matches(before), Outcome = outcome }.MatchInlineSnapshot(
            """
            {
              "Recovered": true,
              "Outcome": {
                "Event": "received",
                "Reply": "Shipped"
              }
            }
            """);
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
        new { Recovered = after.Matches(before), Outcome = outcome }.MatchInlineSnapshot(
            """
            {
              "Recovered": true,
              "Outcome": {
                "Event": "received",
                "Reply": "Shipped"
              }
            }
            """);
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
        new { Detected = detected, Recovered = after.Matches(before), Outcome = outcome }.MatchInlineSnapshot(
            """
            {
              "Detected": true,
              "Recovered": true,
              "Outcome": {
                "Event": "received",
                "Reply": "Shipped"
              }
            }
            """);
    }

    [Fact]
    public async Task StartAsync_Should_Complete_When_BrokerUnreachable()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        string start;
        TestBus? bus = null;

        // act
        try
        {
            await using (await fixture.StopBrokerAsync())
            {
                try
                {
                    bus = await CreateBusAsync(vhost, recorder).WaitAsync(s_timeout, TestContext.Current.CancellationToken);
                    start = "started";
                }
                catch (Exception ex)
                {
                    start = ex.GetType().Name;
                }
            }

            var attached = await Condition.WaitAsync(
                async () => (await vhost.GetStateAsync()).ConsumedQueues == 3,
                s_recoveryTimeout);
            var outcome = bus is null ? null : await RoundTripAsync(bus, recorder, "ORD-1");

            // assert
            new { Start = start, Attached = attached, Outcome = outcome }.MatchInlineSnapshot(
                """
                {
                  "Start": "started",
                  "Attached": true,
                  "Outcome": {
                    "Event": "received",
                    "Reply": "Shipped"
                  }
                }
                """);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.DisposeAsync();
            }
        }
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
        new { Disposed = disposed, Reconnected = reconnected }.MatchInlineSnapshot(
            """
            {
              "Disposed": true,
              "Reconnected": false
            }
            """);
    }

    private static Task<TestBus> CreateBusAsync(
        VhostContext vhost,
        MessageRecorder recorder,
        LogRecorder? logs = null)
        => new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs ?? new LogRecorder()))
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("orders").Handler<OrderCreatedHandler>();
                t.Endpoint("order-status").Handler<GetOrderStatusHandler>();
            })
            .BuildTestBusAsync();

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
}
