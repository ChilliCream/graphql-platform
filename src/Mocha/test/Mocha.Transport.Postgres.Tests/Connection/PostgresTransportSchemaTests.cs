using System.Data;
using System.Transactions;
using CookieCrumble;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Connection;

[Collection("Postgres")]
public class PostgresTransportSchemaTests(PostgresFixture fixture)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MigrateAsync_Should_RecordMigrations_When_DatabaseIsEmpty()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);

        // act
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);

        Assert.Equal(ConnectionState.Open, connection.State);
        migrationHistory.MatchInlineSnapshot(
            """
            [
              "2026-03-06_AddConsumerManagement",
              "2026-03-06_AddTransportIndex",
              "2026-03-06_InitialSchema"
            ]
            """);
    }

    [Fact]
    public async Task GenerateMigrationsSql_Should_MatchDirectMigration_When_ScriptIsAppliedRepeatedly()
    {
        // arrange
        await using var direct = await fixture.CreateDatabaseAsync("ScriptParityDirect");
        await using var scripted = await fixture.CreateDatabaseAsync("ScriptParityScripted");
        await using var connection = await OpenConnectionAsync(direct.ConnectionString);
        var options = new PostgresSchemaOptions();
        await PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);
        var script = PostgresTransportSchema.GenerateMigrationsSql(options);

        // act
        await fixture.RunSqlScriptAsync(script, scripted.DatabaseName);
        await fixture.RunSqlScriptAsync(script, scripted.DatabaseName);
        await using var scriptedConnection = await OpenConnectionAsync(scripted.ConnectionString);
        await PostgresTransportSchema.MigrateAsync(scriptedConnection, options, CancellationToken);
        await fixture.RunSqlScriptAsync(script, direct.DatabaseName);

        // assert
        var directSchema = await ReadSchemaAsync(connection);
        var scriptedSchema = await ReadSchemaAsync(scriptedConnection);
        var directHistory = await ReadMigrationHistoryAsync(connection);
        var scriptedHistory = await ReadMigrationHistoryAsync(scriptedConnection);

        Assert.Equal(directSchema, scriptedSchema);
        Assert.Equal(directHistory, scriptedHistory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_UpgradeAndPreserveData_When_InitialMigrationAlreadyApplied(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();
        await ExecuteNonQueryAsync(connection, PostgresSchemaSql.InitialSchema(options));
        await ExecuteNonQueryAsync(connection,
            """
            CREATE TABLE public.mocha_migrations (
                migration_id text PRIMARY KEY,
                applied_on timestamptz NOT NULL DEFAULT now());
            INSERT INTO public.mocha_migrations (migration_id)
                VALUES ('2026-03-06_InitialSchema'), ('future-migration');
            INSERT INTO public.mocha_queue (name) VALUES ('existing-queue');
            INSERT INTO public.mocha_message (body, queue_id)
                SELECT '\x0102'::bytea, id FROM public.mocha_queue;
            """);

        // act
        await ApplyMigrationsAsync(connection, options, useMigrationScript);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);
        var queueName = await ExecuteScalarAsync<string>(connection, "SELECT name FROM public.mocha_queue;");
        var messageBody = await ExecuteScalarAsync<byte[]>(connection, "SELECT body FROM public.mocha_message;");

        Assert.Equal("existing-queue", queueName);
        Assert.Equal([1, 2], messageBody);
        migrationHistory.MatchInlineSnapshot(
            """
            [
              "2026-03-06_AddConsumerManagement",
              "2026-03-06_AddTransportIndex",
              "2026-03-06_InitialSchema",
              "future-migration"
            ]
            """);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task MigrateAsync_Should_ApplyMigrationsOnce_When_CallersRunConcurrently(
        bool firstUsesScript,
        bool secondUsesScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var firstConnection = await OpenConnectionAsync(database.ConnectionString);
        await using var secondConnection = await OpenConnectionAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();

        // act
        var firstMigration = ApplyMigrationsAsync(firstConnection, options, firstUsesScript);
        var secondMigration = ApplyMigrationsAsync(secondConnection, options, secondUsesScript);
        await Task.WhenAll(firstMigration, secondMigration);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(firstConnection);

        Assert.Equal(3, migrationHistory.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_RollBack_When_MigrationFails(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();
        await ExecuteNonQueryAsync(connection, "CREATE VIEW public.mocha_message AS SELECT 1 AS sentinel;");

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ApplyMigrationsAsync(connection, options, useMigrationScript));

        // assert
        await using var verification = await OpenConnectionAsync(database.ConnectionString);
        var tableCount = await ExecuteScalarAsync<long>(verification,
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public';");
        var sequenceExists = await ExecuteScalarAsync<bool>(verification,
            "SELECT to_regclass('public.mocha_topology_seq') IS NOT NULL;");

        Assert.Equal(PostgresErrorCodes.WrongObjectType, exception.SqlState);
        Assert.Equal(0, tableCount);
        Assert.False(sequenceExists);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_AllowRetry_When_PreviousMigrationFailed(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();
        await ExecuteNonQueryAsync(connection, "CREATE VIEW public.mocha_message AS SELECT 1 AS sentinel;");
        await Assert.ThrowsAsync<PostgresException>(() => ApplyMigrationsAsync(connection, options, useMigrationScript));

        if (useMigrationScript)
        {
            await ExecuteNonQueryAsync(connection, "ROLLBACK;");
        }

        await ExecuteNonQueryAsync(connection, "DROP VIEW public.mocha_message;");

        // act
        await ApplyMigrationsAsync(connection, options, useMigrationScript);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);

        Assert.Equal(3, migrationHistory.Length);
    }

    [Fact]
    public async Task MigrateAsync_Should_AllowRetry_When_CancelledWhileWaitingForMigrationLock()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var blocker = await OpenConnectionAsync(database.ConnectionString);
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        await using var transaction = await blocker.BeginTransactionAsync(CancellationToken);
        await ExecuteNonQueryAsync(blocker, "SELECT pg_advisory_xact_lock(958913715);");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var options = new PostgresSchemaOptions();

        // act
        var migration = PostgresTransportSchema.MigrateAsync(connection, options, cancellation.Token);
        await WaitForMigrationLockAsync(blocker, connection.ProcessID);
        await cancellation.CancelAsync();
        var exception = await Record.ExceptionAsync(() => migration);
        var historyExists = await ExecuteScalarAsync<bool>(connection,
            "SELECT to_regclass('public.mocha_migrations') IS NOT NULL;");

        await transaction.RollbackAsync(CancellationToken);
        await PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);

        // assert
        var migrationHistory = await ReadMigrationHistoryAsync(connection);

        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.False(historyExists);
        Assert.Equal(3, migrationHistory.Length);
    }

    [Theory]
    [InlineData("messaging", "bus_")]
    [InlineData("MixedCase", "Bus_")]
    [InlineData("\"odd.schema'\\\"\"$mocha_migration$\"", "bus_")]
    public async Task MigrateAsync_Should_UseRuntimeNames_When_CustomNamesConfigured(string schema, string prefix)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions { Schema = schema, TablePrefix = prefix };

        // act
        await ApplyMigrationsAsync(connection, options, useMigrationScript: true);
        await ApplyMigrationsAsync(connection, options, useMigrationScript: false);

        // assert
        var migrationCount = await ExecuteScalarAsync<long>(connection,
            $"SELECT count(*) FROM {options.MigrationsTable};");
        var queueCount = await ExecuteScalarAsync<long>(connection,
            $"SELECT count(*) FROM {options.QueueTable};");

        Assert.Equal(3, migrationCount);
        Assert.Equal(0, queueCount);
    }

    [Fact]
    public async Task MigrateAsync_Should_RejectClosedConnection_When_CallerHasNotOpenedIt()
    {
        // arrange
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken));

        // assert
        Assert.Equal("Transport migration requires an open PostgreSQL connection.", exception.Message);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Fact]
    public async Task MigrateAsync_Should_RejectTransaction_When_ConnectionHasActiveTransaction()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        await using var transaction = await connection.BeginTransactionAsync(CancellationToken);
        var options = new PostgresSchemaOptions();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken));

        // assert
        var historyExists = await ExecuteScalarAsync<bool>(connection,
            "SELECT to_regclass('public.mocha_migrations') IS NOT NULL;");

        Assert.Equal("Transport migration requires a connection without an active or ambient transaction.", exception.Message);
        Assert.False(historyExists);
        Assert.Same(connection, transaction.Connection);
    }

    [Fact]
    public async Task MigrateAsync_Should_RejectTransaction_When_AmbientTransactionExists()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        var options = new PostgresSchemaOptions();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken));

        // assert
        var historyExists = await ExecuteScalarAsync<bool>(connection,
            "SELECT to_regclass('public.mocha_migrations') IS NOT NULL;");

        Assert.Equal("Transport migration requires a connection without an active or ambient transaction.", exception.Message);
        Assert.False(historyExists);
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task MigrateAsync_Should_PreserveHistory_When_DatabaseClonedFromTemplate()
    {
        // arrange
        await using var template = await fixture.CreateDatabaseAsync();
        string[] templateHistory;
        await using (var connection = await OpenConnectionAsync(template.ConnectionString))
        {
            await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);
            templateHistory = await ReadMigrationHistoryAsync(connection);
        }

        // act
        await using var clone = await CloneDatabaseAsync(template);

        // assert
        await using var verification = await OpenConnectionAsync(clone.ConnectionString);
        var migrationHistory = await ReadMigrationHistoryAsync(verification);
        var consumerCount = await ExecuteScalarAsync<long>(verification, "SELECT count(*) FROM public.mocha_consumers;");
        var queueCount = await ExecuteScalarAsync<long>(verification, "SELECT count(*) FROM public.mocha_queue;");

        Assert.Equal(0, consumerCount);
        Assert.Equal(0, queueCount);
        Assert.Equal(templateHistory, migrationHistory);
    }

    [Fact]
    public async Task MigrateAsync_Should_LeaveDatabaseUnchanged_When_CancelledBeforeExecution()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), cancellation.Token));

        // assert
        var tableCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public';");

        Assert.Equal(0, tableCount);
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_RejectReplay_When_HistoryWasErased(bool useMigrationScript)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenConnectionAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();
        await PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);
        await ExecuteNonQueryAsync(connection, "TRUNCATE public.mocha_migrations;");

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ApplyMigrationsAsync(connection, options, useMigrationScript));
        if (useMigrationScript)
        {
            await ExecuteNonQueryAsync(connection, "ROLLBACK;");
        }

        // assert
        var migrationCount = await ExecuteScalarAsync<long>(connection,
            "SELECT count(*) FROM public.mocha_migrations;");

        Assert.Equal(PostgresErrorCodes.DuplicateTable, exception.SqlState);
        Assert.Equal(0, migrationCount);
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false, Enlist = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        return connection;
    }

    private static Task ApplyMigrationsAsync(
        NpgsqlConnection connection,
        PostgresSchemaOptions options,
        bool useMigrationScript)
    {
        if (useMigrationScript)
        {
            var sql = PostgresTransportSchema.GenerateMigrationsSql(options);
            return ExecuteNonQueryAsync(connection, sql);
        }

        return PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);
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
    {
        const string sql = "SELECT array_agg(migration_id ORDER BY migration_id) FROM public.mocha_migrations;";
        return ExecuteScalarAsync<string[]>(connection, sql);
    }

    private static Task<string[]> ReadSchemaAsync(NpgsqlConnection connection)
    {
        const string sql =
            """
            SELECT array_agg(description ORDER BY description)
            FROM (
                SELECT 'relation: ' || relname || ' ' || relkind::text AS description
                FROM pg_class WHERE relnamespace = 'public'::regnamespace
                UNION ALL
                SELECT 'column: ' || table_name || '.' || column_name || ' ' || data_type || ' ' || is_nullable
                FROM information_schema.columns WHERE table_schema = 'public'
                UNION ALL
                SELECT 'constraint: ' || conname || ' ' || pg_get_constraintdef(oid)
                FROM pg_constraint WHERE connamespace = 'public'::regnamespace
                UNION ALL
                SELECT 'index: ' || indexdef FROM pg_indexes WHERE schemaname = 'public'
            ) state;
            """;

        return ExecuteScalarAsync<string[]>(connection, sql);
    }

    private static async Task WaitForMigrationLockAsync(NpgsqlConnection connection, int processId)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT FROM pg_locks
                WHERE pid = @process_id AND locktype = 'advisory' AND NOT granted);
            """;
        command.Parameters.AddWithValue("process_id", processId);

        while (true)
        {
            var waitingForLock = await command.ExecuteScalarAsync(timeout.Token);
            if (waitingForLock is true)
            {
                return;
            }

            await Task.Delay(20, timeout.Token);
        }
    }

    private async Task<DatabaseContext> CloneDatabaseAsync(DatabaseContext template)
    {
        var databaseName = $"mocha_clone_{Guid.NewGuid():N}";
        await using var connection = await OpenConnectionAsync(fixture.ConnectionString);
        await ExecuteNonQueryAsync(connection, $"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{template.DatabaseName}\";");

        var settings = new NpgsqlConnectionStringBuilder(template.ConnectionString)
        {
            Database = databaseName
        };
        return new DatabaseContext(fixture, databaseName, settings.ConnectionString);
    }
}
