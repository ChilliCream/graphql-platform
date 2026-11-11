using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.RabbitMQ;
using Npgsql;
using RabbitMQ.Client;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Broker;

public sealed class PostgresOutboxBrokerTests(OutboxBrokerFixture fixture) : IClassFixture<OutboxBrokerFixture>
{
    // A request to the broker that is in flight can take the client 20 seconds to give up.
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(90);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Outbox_Should_KeepMessageAndNotCountAttempts_When_BrokerDoesNotConfirmPublish()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var deliveries = new DeliveryRecorder();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>(LogLevel.Warning);
        await using var host = await StartHostAsync(
            connectionString,
            deliveries,
            logs,
            static o => o.DispatchTimeout = TimeSpan.FromSeconds(2),
            ConsumeOrders);

        // The broker blocks a connection at once only when the connection has published before.
        await PublishAsync(host, "ORD-0");
        await deliveries.WaitForAsync(["ORD-0"], s_timeout, TestToken);
        long openTransactionsWhileBlocked;
        long rowsWhileBlocked;
        int? timesSentWhileBlocked;

        // act
        await using (await fixture.BlockPublishersAsync())
        {
            await PublishAsync(host, "ORD-1");
            await logs.WaitForEntriesAsync(2, s_timeout, TestToken);
            openTransactionsWhileBlocked = await OutboxBrokerFixture.WaitForNoOpenTransactionAsync(
                connectionString, s_timeout, TestToken);
            (rowsWhileBlocked, timesSentWhileBlocked) = await ReadOutboxAsync(connectionString);
        }

        await deliveries.WaitForAsync(["ORD-1"], s_timeout, TestToken);
        var remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // assert
        // a dispatch that was written before the broker blocked the connection can arrive besides the one
        // that is confirmed after the alarm
        Assert.Equal(0, openTransactionsWhileBlocked);
        Assert.Equal(1, rowsWhileBlocked);
        Assert.Equal(0, timesSentWhileBlocked);
        Assert.InRange(deliveries.CountOf("ORD-1"), 1, 2);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Outbox_Should_DeliverMessageAtMostTwice_When_AlarmOutlastsSeveralDispatchTimeouts()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var deliveries = new DeliveryRecorder();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>(LogLevel.Warning);
        await using var host = await StartHostAsync(
            connectionString,
            deliveries,
            logs,
            static o =>
            {
                o.MaxConcurrentMessages = 4;
                o.DispatchTimeout = TimeSpan.FromSeconds(2);
            },
            ConsumeOrders);

        // dispatches side by side leave idle publisher channels, which a later dispatch could write on
        string[] warmUps = [.. Enumerable.Range(0, 8).Select(i => $"WARM-{i}")];
        await Task.WhenAll(warmUps.Select(orderId => PublishAsync(host, orderId)));
        await deliveries.WaitForAsync(warmUps, s_timeout, TestToken);
        await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // act
        await using (await fixture.BlockPublishersAsync())
        {
            await PublishAsync(host, "ORD-1");

            // the alarm outlasts four dispatch timeouts
            await logs.WaitForEntriesAsync(4, s_timeout, TestToken);
        }

        await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // a later message arrives after everything the connection wrote for the first one
        await PublishAsync(host, "ORD-2");
        await deliveries.WaitForAsync(["ORD-2"], s_timeout, TestToken);

        // assert
        // only a dispatch that was written before the broker blocked the connection can arrive besides the
        // one that is confirmed after the alarm
        Assert.InRange(deliveries.CountOf("ORD-1"), 1, 2);
    }

    [Fact]
    public async Task Outbox_Should_DeliverEveryMessage_When_BrokerOutageOutlastsAllAttempts()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var deliveries = new DeliveryRecorder();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>(LogLevel.Warning);
        await using var host = await StartHostAsync(connectionString, deliveries, logs, ExhaustQuickly, ConsumeOrders);
        await PublishAsync(host, "ORD-0");
        await deliveries.WaitForAsync(["ORD-0"], s_timeout, TestToken);
        string[] expected = ["ORD-0", "ORD-1", "ORD-2"];
        long rowsDuringOutage;

        // act
        await using (await fixture.StopBrokerAsync())
        {
            foreach (var orderId in expected[1..])
            {
                await PublishAsync(host, orderId);
            }

            // More dispatches fail than the two messages have attempts together.
            await logs.WaitForEntriesAsync(5, s_timeout, TestToken);
            rowsDuringOutage = await MessageRows.CountOutboxAsync(connectionString, TestToken);
        }

        var delivered = await deliveries.WaitForAsync(expected, s_timeout, TestToken);
        var remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // assert
        Assert.Equal(2, rowsDuringOutage);
        Assert.Equal(expected, delivered);
        Assert.Equal(0, remaining);
        Assert.Equal([LogLevel.Warning], logs.Entries.Select(entry => entry.Level).Distinct());
    }

    [Fact]
    public async Task Outbox_Should_KeepMessageAndNotCountAttempts_When_QueueRejectsMessagesLongerThanAllAttempts()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>(LogLevel.Warning);
        await using var host = await StartHostAsync(
            connectionString,
            new DeliveryRecorder(),
            logs,
            ExhaustQuickly,
            static t =>
            {
                t.DeclareQueue("stock").WithArgument("x-max-length", 1).WithArgument("x-overflow", "reject-publish");
                t.DispatchEndpoint("stock-send").ToQueue("stock").Send<ReserveStock>();
            });
        await using var connection = await host.Services.GetRequiredService<IConnectionFactory>()
            .CreateConnectionAsync(TestToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestToken);
        await SendAsync(host, "ORD-0");
        await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // act
        // the queue holds one message and rejects every further one
        await SendAsync(host, "ORD-1");

        // More dispatches time out than the message has attempts.
        await logs.WaitForEntriesAsync(3, s_timeout, TestToken);
        var (rowsWhileFull, timesSentWhileFull) = await ReadOutboxAsync(connectionString);
        await channel.BasicGetAsync("stock", autoAck: true, TestToken);
        var remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        var queued = await channel.MessageCountAsync("stock", TestToken);

        // assert
        Assert.Equal(1, rowsWhileFull);
        Assert.Equal(0, timesSentWhileFull);
        Assert.Equal(0, remaining);
        Assert.Equal(1u, queued);
        Assert.Equal([LogLevel.Warning], logs.Entries.Select(entry => entry.Level).Distinct());
    }

    private static void ExhaustQuickly(PostgresOutboxOptions options)
    {
        options.DispatchTimeout = TimeSpan.FromSeconds(2);
        options.MaxAttempts = 2;
        options.MaxRetryDelay = TimeSpan.FromMilliseconds(200);
    }

    private static void ConsumeOrders(IRabbitMQMessagingTransportDescriptor transport)
        => transport.Endpoint("orders").Handler<OrderPlacedHandler>();

    private async Task<BrokerHost> StartHostAsync(
        string connectionString,
        DeliveryRecorder deliveries,
        RecordingLoggerProvider logs,
        Action<PostgresOutboxOptions> configure,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        services.AddSingleton(await fixture.CreateVirtualHostAsync());
        services.AddSingleton(deliveries);
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox(o =>
        {
            o.RequireTransaction = false;
            configure(o);
        }));
        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddRabbitMQ(configureTransport);

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        }

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var service in hostedServices)
        {
            await service.StartAsync(TestToken);
        }

        return new BrokerHost(provider, runtime, hostedServices);
    }

    private static async Task PublishAsync(BrokerHost host, string orderId)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderPlaced(orderId), TestToken);
    }

    private static async Task SendAsync(BrokerHost host, string orderId)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.SendAsync(new ReserveStock(orderId), TestToken);
    }

    private static async Task<(long Rows, int? TimesSent)> ReadOutboxAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT COUNT(*), MAX("times_sent") FROM "outbox_messages";""";
        await using var reader = await command.ExecuteReaderAsync(TestToken);
        await reader.ReadAsync(TestToken);
        return (reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt32(1));
    }

    public sealed record OrderPlaced(string OrderId);

    public sealed record ReserveStock(string OrderId);

    public sealed class OrderPlacedHandler(DeliveryRecorder deliveries) : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        {
            deliveries.Record(message.OrderId);
            return default;
        }
    }

    public sealed class DeliveryRecorder
    {
        private readonly ConcurrentQueue<string> _orderIds = new();

        public void Record(string orderId) => _orderIds.Enqueue(orderId);

        /// <summary>
        /// Gets how often an order was delivered.
        /// </summary>
        public int CountOf(string orderId) => _orderIds.Count(id => id == orderId);

        /// <summary>
        /// Waits until every expected order was delivered at least once and returns every delivery so far,
        /// in order, so an order that was delivered twice appears twice.
        /// </summary>
        public async Task<string[]> WaitForAsync(
            string[] expected,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + timeout;

            while (expected.Except(_orderIds).Any() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20, cancellationToken);
            }

            return [.. _orderIds.Order()];
        }
    }

    private sealed class BrokerHost(
        ServiceProvider provider,
        MessagingRuntime runtime,
        List<IHostedService> hostedServices) : IAsyncDisposable
    {
        public IServiceProvider Services => provider;

        public async ValueTask DisposeAsync()
        {
            // A host stops with a shutdown timeout, after which work that still runs is cancelled.
            using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            for (var i = hostedServices.Count - 1; i >= 0; i--)
            {
                await hostedServices[i].StopAsync(shutdownTimeout.Token);
            }

            foreach (var transport in runtime.Transports)
            {
                if (transport.IsStarted)
                {
                    await transport.StopAsync(runtime, shutdownTimeout.Token);
                }
            }

            await provider.DisposeAsync();
        }
    }
}
