using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxUnresolvableMessageTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Outbox_Should_RetryMessage_When_MessageTypeIsUnknown()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var host = await StartHostAsync(connectionString);

        // The row mimics a message written by a newer instance whose type this host does not know.
        var id = await MessageRows.InsertOutboxAsync(
            connectionString,
            new MessageEnvelope
            {
                MessageId = Guid.NewGuid().ToString(),
                MessageType = "urn:message:Mocha.Tests:UnknownEvent",
                DestinationAddress = "memory://localhost/q/unknown"
            },
            TestContext.Current.CancellationToken);

        // act
        host.Services.GetRequiredKeyedService<IOutboxSignal>(typeof(TestDbContext)).Set();

        // assert
        var timesSent = await MessageRows.WaitForOutboxRetryAsync(
            connectionString,
            id,
            s_timeout,
            TestContext.Current.CancellationToken);
        Assert.True(timesSent is >= 2, $"Expected a retried row, but times_sent was {timesSent?.ToString() ?? "missing"}.");
    }

    [Fact]
    public async Task Outbox_Should_RetryMessage_When_DestinationIsUnresolvable()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var host = await StartHostAsync(connectionString);

        var runtime = host.Services.GetRequiredService<IMessagingRuntime>();
        var knownType = runtime.GetMessageType(typeof(KnownEvent));

        var id = await MessageRows.InsertOutboxAsync(
            connectionString,
            new MessageEnvelope
            {
                MessageId = Guid.NewGuid().ToString(),
                MessageType = knownType.Identity,
                DestinationAddress = "nowhere://unknown"
            },
            TestContext.Current.CancellationToken);

        // act
        host.Services.GetRequiredKeyedService<IOutboxSignal>(typeof(TestDbContext)).Set();

        // assert
        var timesSent = await MessageRows.WaitForOutboxRetryAsync(
            connectionString,
            id,
            s_timeout,
            TestContext.Current.CancellationToken);
        Assert.True(timesSent is >= 2, $"Expected a retried row, but times_sent was {timesSent?.ToString() ?? "missing"}.");
    }

    private static async Task<MessagingTestHost> StartHostAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<KnownEventHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        return await MessagingTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
    }

    public sealed class KnownEvent;

    public sealed class KnownEventHandler : IEventHandler<KnownEvent>
    {
        public ValueTask HandleAsync(KnownEvent message, CancellationToken cancellationToken)
        {
            return default;
        }
    }
}
