using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class TransportHealthTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task GetHealth_Should_ReportDegradedConsumer_When_QueueMissingWithoutAutoProvision()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await DeclareQueueAsync(vhost, "orders");
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("orders").AutoProvision(false);
                t.Endpoint("orders").Handler<OrderCreatedHandler>();
            })
            .BuildTestBusAsync();
        var transport = GetTransport(bus);
        var before = Summarize(transport.GetHealth());

        // act
        await vhost.DeleteQueueAsync("orders");
        var degraded = await Condition.WaitAsync(
            () => FindEntry(transport.GetHealth(), "consumer:orders").Status == TransportHealthStatus.Degraded,
            s_timeout);
        // the entry reports the error of the first failed re-attach
        var reportsError = await Condition.WaitAsync(
            () => FindEntry(transport.GetHealth(), "consumer:orders").Exception is not null,
            s_timeout);
        var whileMissing = FindEntry(transport.GetHealth(), "consumer:orders");
        await DeclareQueueAsync(vhost, "orders");
        var recovered = await Condition.WaitAsync(
            () => transport.GetHealth().Status == TransportHealthStatus.Healthy,
            TimeSpan.FromSeconds(20));

        // assert
        new
        {
            Before = before,
            Degraded = degraded,
            ReportsError = reportsError,
            WhileMissing = new
            {
                whileMissing.Status,
                HasSince = whileMissing.Since is not null
            },
            Recovered = recovered
        }.MatchInlineSnapshot(
            """
            {
              "Before": {
                "Status": "Healthy",
                "Entries": {
                  "connection:consumer": "Healthy",
                  "connection:dispatcher": "Healthy",
                  "consumer:orders": "Healthy",
                  "consumer:{reply}": "Healthy"
                }
              },
              "Degraded": true,
              "ReportsError": true,
              "WhileMissing": {
                "Status": "Degraded",
                "HasSince": true
              },
              "Recovered": true
            }
            """);
    }

    [Fact]
    public async Task GetHealth_Should_ReportDegradedConnections_When_BrokerStopped()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        var transport = GetTransport(bus);

        // act
        TransportHealth whileStopped;
        await using (await fixture.StopBrokerAsync())
        {
            await Condition.WaitAsync(
                () => FindEntry(transport.GetHealth(), "connection:consumer").Status
                    == TransportHealthStatus.Degraded,
                s_timeout);
            whileStopped = transport.GetHealth();
        }

        var recovered = await Condition.WaitAsync(
            () => transport.GetHealth().Status == TransportHealthStatus.Healthy,
            TimeSpan.FromSeconds(20));

        // assert
        new { WhileStopped = Summarize(whileStopped), Recovered = recovered }.MatchInlineSnapshot(
            """
            {
              "WhileStopped": {
                "Status": "Degraded",
                "Entries": {
                  "connection:consumer": "Degraded",
                  "connection:dispatcher": "Degraded",
                  "consumer:orders": "Degraded",
                  "consumer:{reply}": "Degraded"
                }
              },
              "Recovered": true
            }
            """);
    }

    [Fact]
    public async Task GetHealth_Should_ReportBlockedDispatcher_When_PublishersBlocked()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        var transport = GetTransport(bus);

        // act
        // the broker blocks a connection when it publishes during the alarm
        TransportHealthEntry whileBlocked;
        await using (await fixture.BlockPublishersAsync())
        {
            await TryPublishAsync(bus, TimeSpan.FromSeconds(2));
            await Condition.WaitAsync(
                () => FindEntry(transport.GetHealth(), "connection:dispatcher").Status
                    == TransportHealthStatus.Degraded,
                s_timeout);
            whileBlocked = FindEntry(transport.GetHealth(), "connection:dispatcher");
        }

        var unblocked = await Condition.WaitAsync(
            () => transport.GetHealth().Status == TransportHealthStatus.Healthy,
            s_timeout);

        // assert
        new
        {
            whileBlocked.Status,
            Blocked = whileBlocked.Description.StartsWith("Blocked by the broker", StringComparison.Ordinal),
            Unblocked = unblocked
        }.MatchInlineSnapshot(
            """
            {
              "Status": "Degraded",
              "Blocked": true,
              "Unblocked": true
            }
            """);
    }

    private static RabbitMQMessagingTransport GetTransport(TestBus bus)
        => bus.Provider
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>()
            .Single();

    private static TransportHealthEntry FindEntry(TransportHealth health, string name)
        => health.Entries.Single(entry => entry.Name == name);

    private static object Summarize(TransportHealth health)
        => new
        {
            health.Status,
            // the reply queue name is unique per instance
            Entries = health.Entries
                .OrderBy(entry => entry.Name, StringComparer.Ordinal)
                .ToDictionary(
                    entry => entry.Name.StartsWith("consumer:response-", StringComparison.Ordinal)
                        ? "consumer:{reply}"
                        : entry.Name,
                    entry => entry.Status.ToString())
        };

    private static async Task TryPublishAsync(TestBus bus, TimeSpan timeout)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>()
                .PublishAsync(new OrderCreated { OrderId = "ORD-1" }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // The publish waits for its confirm while the connection is blocked.
        }
    }

    private static async Task DeclareQueueAsync(VhostContext vhost, string queueName)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
