using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Connection;

[Collection("Postgres")]
public class PostgresTransportSchemaTests(PostgresFixture fixture)
{
    private readonly PostgresSchemaOptions _schemaOptions = new() { Schema = "messaging" };
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StartAsync_Should_CreateSchema_When_AutoMigrateEnabledAndSchemaPermissionsGranted()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var services = new ServiceCollection();
        services.AddMessageBus()
            .AddPostgres(transport =>
            {
                transport.ConnectionString(database.ConnectionString);
                transport.ExtendWith(extension => extension.Configuration.SchemaOptions = _schemaOptions);
            });
        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        await using var bus = new TestBus(provider, runtime);

        // act
        await runtime.StartAsync(CancellationToken);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);

        Assert.Equal(
            ["2026-03-06_AddConsumerManagement", "2026-03-06_AddTransportIndex", "2026-03-06_InitialSchema"],
            migrationHistory);
    }

    [Fact]
    public async Task StartAsync_Should_Fail_When_AutoMigrateEnabledWithoutSchemaPermissions()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var role = $"mocha_runtime_{Guid.NewGuid():N}";
        await ExecuteNonQueryAsync(connection, $"CREATE ROLE {role} LOGIN PASSWORD 'runtime-test';");

        try
        {
            await ExecuteNonQueryAsync(connection,
                $"""
                GRANT CONNECT ON DATABASE "{database.DatabaseName}" TO {role};
                """);
            var settings = new NpgsqlConnectionStringBuilder(database.ConnectionString)
            {
                Username = role,
                Password = "runtime-test",
                Pooling = false
            };
            var services = new ServiceCollection();
            services.AddMessageBus()
                .AddPostgres(transport =>
                {
                    transport.ConnectionString(settings.ConnectionString);
                    transport.ExtendWith(extension => extension.Configuration.SchemaOptions = _schemaOptions);
                });
            var provider = services.BuildServiceProvider();
            var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
            await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
            await using var bus = new TestBus(provider, runtime);

            // act
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => runtime.StartAsync(CancellationToken).AsTask());

            // assert
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
        }
        finally
        {
            await ExecuteNonQueryAsync(connection, $"DROP OWNED BY {role}; DROP ROLE {role};");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_CreateResources_When_DatabaseIsEmpty(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);

        // act
        await ApplyMigrationsAsync(connection, useMigrationScript);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);
        var tables = await ReadTableNamesAsync(connection);
        var sequences = await ReadSequenceNamesAsync(connection);

        Assert.Equal(
            ["2026-03-06_AddConsumerManagement", "2026-03-06_AddTransportIndex", "2026-03-06_InitialSchema"],
            migrationHistory);
        Assert.Equal(
            [
                "mocha_consumers",
                "mocha_message",
                "mocha_migrations",
                "mocha_queue",
                "mocha_queue_subscription",
                "mocha_topic"
            ],
            tables);
        Assert.Equal(["mocha_topology_seq"], sequences);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_LeaveResourcesUnchanged_When_RunTwice(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        await ApplyMigrationsAsync(connection, useMigrationScript);
        var tables = await ReadTableNamesAsync(connection);
        var migrationHistory = await ReadMigrationHistoryAsync(connection);

        // act
        await ApplyMigrationsAsync(connection, useMigrationScript);

        // assert
        var repeatedTables = await ReadTableNamesAsync(connection);
        var repeatedMigrationHistory = await ReadMigrationHistoryAsync(connection);

        Assert.Equal(tables, repeatedTables);
        Assert.Equal(migrationHistory, repeatedMigrationHistory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_ApplyPendingMigrations_When_OnlyInitialMigrationIsRecorded(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        await ExecuteNonQueryAsync(connection, PostgresSchemaSql.InitialSchema(_schemaOptions));
        await ExecuteNonQueryAsync(connection,
            """
            CREATE TABLE messaging.mocha_migrations (
                migration_id text PRIMARY KEY,
                applied_on timestamptz NOT NULL DEFAULT now());
            INSERT INTO messaging.mocha_migrations (migration_id) VALUES ('2026-03-06_InitialSchema');
            """);
        var tables = await ReadTableNamesAsync(connection);

        // act
        await ApplyMigrationsAsync(connection, useMigrationScript);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);
        var migratedTables = await ReadTableNamesAsync(connection);
        var messageIndexes = await ReadMessageIndexNamesAsync(connection);
        var queueColumns = await ReadQueueColumnNamesAsync(connection);

        Assert.Equal(
            ["2026-03-06_AddConsumerManagement", "2026-03-06_AddTransportIndex", "2026-03-06_InitialSchema"],
            migrationHistory);
        Assert.Equal(["mocha_consumers"], migratedTables.Except(tables));
        Assert.Equal(
            [
                "mocha_message_expiration_scheduled_ndx",
                "mocha_message_pkey",
                "mocha_message_queue_ndx",
                "mocha_message_sent_time_ndx",
                "mocha_message_transport_queue_ndx"
            ],
            messageIndexes);
        Assert.Equal(
            [
                "id",
                "updated",
                "name",
                "consumer_id"
            ],
            queueColumns);
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_ScriptAppliedAndAutoMigrateDisabled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var script = PostgresTransportSchema.GenerateMigrationsSql(_schemaOptions);
        await ExecuteNonQueryAsync(connection, script);
        var role = $"mocha_runtime_{Guid.NewGuid():N}";
        await ExecuteNonQueryAsync(connection, $"CREATE ROLE {role} LOGIN PASSWORD 'runtime-test';");

        try
        {
            await ExecuteNonQueryAsync(connection,
                $"""
                GRANT CONNECT ON DATABASE "{database.DatabaseName}" TO {role};
                GRANT USAGE ON SCHEMA messaging TO {role};
                GRANT SELECT, INSERT, UPDATE, DELETE ON
                    messaging.mocha_topic, messaging.mocha_queue, messaging.mocha_queue_subscription,
                    messaging.mocha_message, messaging.mocha_consumers TO {role};
                GRANT USAGE ON SEQUENCE messaging.mocha_topology_seq TO {role};
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
                .AddPostgres(transport =>
                {
                    transport.ConnectionString(settings.ConnectionString).AutoMigrate(false);
                    transport.ExtendWith(extension => extension.Configuration.SchemaOptions = _schemaOptions);
                })
                .BuildTestBusAsync();
            using var scope = bus.Provider.CreateScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // act
            await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken);
            var delivered = await recorder.WaitAsync(TimeSpan.FromSeconds(30));

            // assert
            Assert.True(delivered, "Handler did not receive the event.");
            var order = Assert.Single(recorder.Messages.Cast<OrderCreated>());
            Assert.Equal("ORD-1", order.OrderId);
        }
        finally
        {
            await ExecuteNonQueryAsync(connection, $"DROP OWNED BY {role}; DROP ROLE {role};");
        }
    }

    [Fact]
    public async Task MigrateAsync_Should_RollBack_When_MigrationFails()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        // The conflicting view deliberately makes index creation fail after earlier resources have been created.
        await ExecuteNonQueryAsync(connection,
            """
            CREATE SCHEMA messaging;
            CREATE VIEW messaging.mocha_message AS SELECT 1 AS sentinel;
            """);

        // act
        await Assert.ThrowsAsync<PostgresException>(
            () => PostgresTransportSchema.MigrateAsync(connection, _schemaOptions, CancellationToken));

        // assert
        var relations = await ReadRelationNamesAsync(connection);

        Assert.Equal(["mocha_message"], relations);
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false, Enlist = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        return connection;
    }

    private Task ApplyMigrationsAsync(NpgsqlConnection connection, bool useMigrationScript)
    {
        if (useMigrationScript)
        {
            return ExecuteNonQueryAsync(connection, PostgresTransportSchema.GenerateMigrationsSql(_schemaOptions));
        }

        return PostgresTransportSchema.MigrateAsync(connection, _schemaOptions, CancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(NpgsqlConnection connection, string sql)
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

    private static Task<string[]> ReadMigrationHistoryAsync(NpgsqlConnection connection)
        => ExecuteScalarAsync<string[]>(connection,
            "SELECT array_agg(migration_id ORDER BY migration_id) FROM messaging.mocha_migrations;");

    private static Task<string[]> ReadTableNamesAsync(NpgsqlConnection connection)
        => ExecuteScalarAsync<string[]>(connection,
            """
            SELECT array_agg(table_name ORDER BY table_name)
            FROM information_schema.tables
            WHERE table_schema = 'messaging' AND table_type = 'BASE TABLE';
            """);

    private static Task<string[]> ReadSequenceNamesAsync(NpgsqlConnection connection)
        => ExecuteScalarAsync<string[]>(connection,
            """
            SELECT array_agg(sequence_name ORDER BY sequence_name)
            FROM information_schema.sequences
            WHERE sequence_schema = 'messaging';
            """);

    private static Task<string[]> ReadMessageIndexNamesAsync(NpgsqlConnection connection)
        => ExecuteScalarAsync<string[]>(connection,
            """
            SELECT array_agg(indexname ORDER BY indexname)
            FROM pg_indexes
            WHERE schemaname = 'messaging' AND tablename = 'mocha_message';
            """);

    private static Task<string[]> ReadQueueColumnNamesAsync(NpgsqlConnection connection)
        => ExecuteScalarAsync<string[]>(connection,
            """
            SELECT array_agg(column_name ORDER BY ordinal_position)
            FROM information_schema.columns
            WHERE table_schema = 'messaging' AND table_name = 'mocha_queue';
            """);

    private static Task<string[]> ReadRelationNamesAsync(NpgsqlConnection connection)
        => ExecuteScalarAsync<string[]>(connection,
            """
            SELECT array_agg(relname ORDER BY relname)
            FROM pg_class
            WHERE relnamespace = 'messaging'::regnamespace;
            """);
}
