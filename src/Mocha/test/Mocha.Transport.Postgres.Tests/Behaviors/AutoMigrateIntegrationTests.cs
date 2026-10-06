using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class AutoMigrateIntegrationTests(PostgresFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    public async Task StartAsync_Should_CreateSchema_When_AutoMigrateEnabled(bool? autoMigrate)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var services = new ServiceCollection();
        services.AddMessageBus().AddPostgres(transport =>
        {
            transport.ConnectionString(database.ConnectionString);
            transport.AutoProvision(false);

            if (autoMigrate.HasValue)
            {
                transport.AutoMigrate(autoMigrate.Value);
            }
        });

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        await using var bus = new TestBus(provider, runtime);

        // act
        await runtime.StartAsync(CancellationToken);

        // assert
        var migrationCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM public.mocha_migrations;");
        var consumerCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM public.mocha_consumers;");

        Assert.Equal(3, migrationCount);
        Assert.Equal(1, consumerCount);
    }

    [Fact]
    public async Task StartAsync_Should_UpgradeSchema_When_InitialMigrationAlreadyApplied()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await ExecuteAsync(connection, PostgresSchemaSql.InitialSchema(new PostgresSchemaOptions()));
        await ExecuteAsync(connection,
            """
            CREATE TABLE public.mocha_migrations (
                migration_id text PRIMARY KEY,
                applied_on timestamptz NOT NULL DEFAULT now());
            INSERT INTO public.mocha_migrations (migration_id) VALUES ('2026-03-06_InitialSchema');
            """);

        var services = new ServiceCollection();
        services.AddMessageBus().AddPostgres(transport =>
        {
            transport.ConnectionString(database.ConnectionString);
            transport.AutoProvision(false);
        });

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        await using var bus = new TestBus(provider, runtime);

        // act
        await runtime.StartAsync(CancellationToken);

        // assert
        var migrationCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM public.mocha_migrations;");
        var consumerCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM public.mocha_consumers;");

        Assert.Equal(3, migrationCount);
        Assert.Equal(1, consumerCount);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverWithoutMigrationHistory_When_AutoMigrateDisabled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);
        await ExecuteAsync(connection, "DROP TABLE public.mocha_migrations;");

        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(transport => transport.ConnectionString(database.ConnectionString).AutoMigrate(false))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken);
        var delivered = await recorder.WaitAsync(s_timeout);

        // assert
        var historyExists = await ExecuteScalarAsync<bool>(connection,
            "SELECT to_regclass('public.mocha_migrations') IS NOT NULL;");

        Assert.True(delivered, "Handler did not receive the event.");
        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-1", order.OrderId);
        Assert.False(historyExists);
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_RuntimeHasOnlyDmlPermissions()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);
        var role = $"mocha_runtime_{Guid.NewGuid():N}";
        await ExecuteAsync(connection, $"CREATE ROLE {role} LOGIN PASSWORD 'runtime-test';");

        try
        {
            await ExecuteAsync(connection,
                $"""
                GRANT CONNECT ON DATABASE "{database.DatabaseName}" TO {role};
                GRANT USAGE ON SCHEMA public TO {role};
                GRANT SELECT, INSERT, UPDATE, DELETE ON
                    public.mocha_topic, public.mocha_queue, public.mocha_queue_subscription,
                    public.mocha_message, public.mocha_consumers TO {role};
                GRANT USAGE ON SEQUENCE public.mocha_topology_seq TO {role};
                """);
            var settings = new NpgsqlConnectionStringBuilder(database.ConnectionString)
            {
                Username = role,
                Password = "runtime-test",
                Pooling = false
            };
            await using var bus = await new ServiceCollection()
                .AddSingleton(recorder)
                .AddMessageBus()
                .AddEventHandler<OrderCreatedHandler>()
                .AddPostgres(transport => transport.ConnectionString(settings.ConnectionString).AutoMigrate(false))
                .BuildTestBusAsync();

            using var scope = bus.Provider.CreateScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // act
            await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken);
            var delivered = await recorder.WaitAsync(s_timeout);

            // assert
            var canCreateSchema = await ExecuteScalarAsync<bool>(connection,
                $"SELECT has_schema_privilege('{role}', 'public', 'CREATE');");
            var canReadHistory = await ExecuteScalarAsync<bool>(connection,
                $"SELECT has_table_privilege('{role}', 'public.mocha_migrations', 'SELECT');");

            Assert.True(delivered, "Handler did not receive the event.");
            var order = Assert.Single(recorder.Messages.OfType<OrderCreated>());
            Assert.Equal("ORD-1", order.OrderId);
            Assert.False(canCreateSchema);
            Assert.False(canReadHistory);
        }
        finally
        {
            await ExecuteAsync(connection, $"DROP OWNED BY {role}; DROP ROLE {role};");
        }
    }

    [Fact]
    public async Task StartAsync_Should_Fail_When_AutoMigrateDisabledAndDatabaseIsEmpty()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var services = new ServiceCollection();
        services.AddMessageBus().AddPostgres(transport =>
            transport.ConnectionString(database.ConnectionString).AutoMigrate(false));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        await using var bus = new TestBus(provider, runtime);

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => runtime.StartAsync(CancellationToken).AsTask());

        // assert
        var tableCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public';");

        Assert.Equal(PostgresErrorCodes.UndefinedTable, exception.SqlState);
        Assert.Equal("relation \"public.mocha_consumers\" does not exist", exception.MessageText);
        Assert.Equal(0, tableCount);
    }

    private static NpgsqlConnection CreateSetupConnection(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false };
        return new NpgsqlConnection(settings.ConnectionString);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<T> ExecuteScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(CancellationToken);
        return (T)result!;
    }
}
