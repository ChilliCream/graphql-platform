using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.Postgres;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresMigrationOutboxTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishAsync_Should_PersistOutboxMessage_When_DatabasePreparedBeforeRuntime(bool cloneFromTemplate)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await CreateMigratedDatabaseAsync(cancellationToken);
        if (cloneFromTemplate)
        {
            connectionString = await CloneDatabaseAsync(connectionString, cancellationToken);
        }

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();
        services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox())
            .AddPostgres(transport =>
            {
                transport.ConnectionString(connectionString);
                transport.AutoMigrate(false);
                transport.DeclareTopic("prepared-events");
                transport.DispatchEndpoint("prepared-events").ToTopic("prepared-events").Publish<PreparedEvent>();
            });

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var expectedEvent = new PreparedEvent("persisted-before-dispatch");
        await runtime.StartAsync(cancellationToken);

        try
        {
            // act
            await using (var scope = provider.CreateAsyncScope())
            {
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await messageBus.PublishAsync(expectedEvent, cancellationToken);
            }

            // assert
            await using var verification = provider.CreateAsyncScope();
            var context = verification.ServiceProvider.GetRequiredService<TestDbContext>();
            var message = await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(cancellationToken);
            var body = message.Envelope.RootElement.GetProperty("body");
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var persistedEvent = body.Deserialize<PreparedEvent>(jsonOptions);

            Assert.Equal(expectedEvent, persistedEvent);
            Assert.Equal(0, message.TimesSent);
        }
        finally
        {
            await transport.StopAsync(runtime, CancellationToken.None);
        }
    }

    private async Task<string> CreateMigratedDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false };
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseTestNpgsql(settings.ConnectionString)
            .Options;
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), cancellationToken);

        return settings.ConnectionString;
    }

    private static async Task<string> CloneDatabaseAsync(
        string templateConnectionString,
        CancellationToken cancellationToken)
    {
        var template = new NpgsqlConnectionStringBuilder(templateConnectionString);
        var cloneName = $"mocha_clone_{Guid.NewGuid():N}";
        var adminSettings = new NpgsqlConnectionStringBuilder(templateConnectionString) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(adminSettings.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{cloneName}\" TEMPLATE \"{template.Database}\";";
        await command.ExecuteNonQueryAsync(cancellationToken);

        var cloneSettings = new NpgsqlConnectionStringBuilder(templateConnectionString) { Database = cloneName };
        return cloneSettings.ConnectionString;
    }

    public sealed record PreparedEvent(string Payload);
}
