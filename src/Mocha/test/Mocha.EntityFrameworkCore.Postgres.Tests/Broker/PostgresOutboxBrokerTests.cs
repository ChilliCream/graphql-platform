using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.RabbitMQ;
using RabbitMQ.Client;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Broker;

public sealed class PostgresOutboxBrokerTests(OutboxBrokerFixture fixture) : IClassFixture<OutboxBrokerFixture>
{
    // A request to the broker that is in flight can take the client 20 seconds to give up.
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(90);

    // Long enough for the outbox to dispatch a message that the broker can take.
    private static readonly TimeSpan s_dispatchWindow = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Outbox_Should_KeepMessage_When_BrokerDoesNotConfirmPublish()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var deliveries = new DeliveryRecorder();
        await using var host = await StartHostAsync(connectionString, deliveries, ConsumeOrders);

        // The broker blocks a connection at once only when the connection has published before.
        await PublishAsync(host, "ORD-0");
        await deliveries.WaitForAsync(["ORD-0"], s_timeout, TestToken);
        long rowsWhileBlocked;

        // act
        await using (await fixture.BlockPublishersAsync())
        {
            await PublishAsync(host, "ORD-1");
            await Task.Delay(s_dispatchWindow, TestToken);
            rowsWhileBlocked = await MessageRows.CountOutboxAsync(connectionString, TestToken);
        }

        await deliveries.WaitForAsync(["ORD-1"], s_timeout, TestToken);
        var remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // assert
        // a dispatch that was written before the broker blocked the connection can arrive besides the one
        // that is confirmed after the alarm
        Assert.Equal(1, rowsWhileBlocked);
        Assert.InRange(deliveries.CountOf("ORD-1"), 1, 2);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Outbox_Should_DeliverEveryMessage_When_BrokerWasUnavailable()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var deliveries = new DeliveryRecorder();
        await using var host = await StartHostAsync(connectionString, deliveries, ConsumeOrders);
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

            await Task.Delay(s_dispatchWindow, TestToken);
            rowsDuringOutage = await MessageRows.CountOutboxAsync(connectionString, TestToken);
        }

        var delivered = await deliveries.WaitForAsync(expected, s_timeout, TestToken);
        var remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);

        // assert
        Assert.Equal(2, rowsDuringOutage);
        Assert.Equal(expected, delivered);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Outbox_Should_KeepMessage_When_QueueRejectsMessages()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var host = await StartHostAsync(
            connectionString,
            new DeliveryRecorder(),
            static t =>
            {
                t.PublishTimeout(TimeSpan.FromSeconds(2));
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
        await Task.Delay(s_dispatchWindow, TestToken);
        var rowsWhileFull = await MessageRows.CountOutboxAsync(connectionString, TestToken);
        await channel.BasicGetAsync("stock", autoAck: true, TestToken);
        var remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        var queued = await channel.MessageCountAsync("stock", TestToken);

        // assert
        Assert.Equal(1, rowsWhileFull);
        Assert.Equal(0, remaining);
        Assert.Equal(1u, queued);
    }

    private static void ConsumeOrders(IRabbitMQMessagingTransportDescriptor transport)
        => transport.Endpoint("orders").Handler<OrderPlacedHandler>();

    private async Task<BrokerHost> StartHostAsync(
        string connectionString,
        DeliveryRecorder deliveries,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(await fixture.CreateVirtualHostAsync());
        services.AddSingleton(deliveries);
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox(o => o.RequireTransaction = false));
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
