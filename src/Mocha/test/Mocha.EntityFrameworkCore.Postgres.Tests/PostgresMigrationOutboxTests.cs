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
    public async Task PublishAsync_Should_PersistOutboxMessage_When_DatabasePreparedBeforeRuntime(bool template)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var source = new NpgsqlConnectionStringBuilder(await fixture.CreateDatabaseAsync()) { Pooling = false };
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(source.ConnectionString).Options;
        await using (var db = new TestDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(ct);
            await db.Database.OpenConnectionAsync(ct);
            await PostgresTransportSchema.MigrateAsync(
                (NpgsqlConnection)db.Database.GetDbConnection(), new PostgresSchemaOptions(), ct);
        }

        var runtimeConnection = source.ConnectionString;
        if (template)
        {
            var clone = new NpgsqlConnectionStringBuilder(await fixture.CreateDatabaseAsync()) { Pooling = false };
            var adminSettings = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres" };
            await using var admin = new NpgsqlConnection(adminSettings.ConnectionString);
            await admin.OpenAsync(ct);
            await using var command = admin.CreateCommand();
            command.CommandText = $"DROP DATABASE \"{clone.Database}\";";
            await command.ExecuteNonQueryAsync(ct);
            command.CommandText = $"CREATE DATABASE \"{clone.Database}\" TEMPLATE \"{source.Database}\";";
            await command.ExecuteNonQueryAsync(ct);
            runtimeConnection = clone.ConnectionString;
        }

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(runtimeConnection));
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();
        services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox())
            .AddPostgres(t =>
            {
                t.ConnectionString(runtimeConnection).AutoMigrate(false).AutoProvision(true);
                t.DeclareTopic("prepared-events");
                t.DispatchEndpoint("prepared-events").ToTopic("prepared-events").Publish<PreparedEvent>();
            });
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        await runtime.StartAsync(ct);
        try
        {
            // act
            using (var scope = provider.CreateScope())
            {
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(new PreparedEvent("persisted-before-dispatch"), ct);
            }

            // assert
            using var verification = provider.CreateScope();
            var db = verification.ServiceProvider.GetRequiredService<TestDbContext>();
            var message = await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(ct);
            var body = message.Envelope.RootElement.GetProperty("body");
            var persisted = body.Deserialize<PreparedEvent>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.Equal(new PreparedEvent("persisted-before-dispatch"), persisted);
            Assert.Equal(0, message.TimesSent);
            await db.Database.OpenConnectionAsync(ct);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT count(*) FROM public.mocha_migrations;";
            Assert.Equal(3L, await command.ExecuteScalarAsync(ct));
        }
        finally
        {
            await transport.StopAsync(runtime, CancellationToken.None);
        }
    }

    public sealed record PreparedEvent(string Payload);
}
