using System.Data;
using System.Transactions;
using CookieCrumble;
using Mocha.Transport.Postgres.Tests.Behaviors;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Connection;

[Collection("Postgres")]
public class PostgresTransportSchemaTests(PostgresFixture fixture)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MigrateAsync_Should_CreateTransportSchema_When_DatabaseIsEmpty()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);

        // act
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);

        // assert
        Assert.Equal(ConnectionState.Open, connection.State);
        (await ReadSchemaAsync(connection)).MatchSnapshot();
        (await ReadHistoryAsync(connection)).MatchInlineSnapshot(
            """
            [
              "2026-03-06_AddConsumerManagement",
              "2026-03-06_AddTransportIndex",
              "2026-03-06_InitialSchema"
            ]
            """);
    }

    [Fact]
    public async Task GenerateMigrationScript_Should_MatchDirectMigration_When_AppliedWithPsql()
    {
        // arrange
        await using var direct = await fixture.CreateDatabaseAsync("ScriptParityDirect");
        await using var scripted = await fixture.CreateDatabaseAsync("ScriptParityScripted");
        await using var connection = await OpenAsync(direct.ConnectionString);
        var options = new PostgresSchemaOptions();
        await PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);
        var script = PostgresTransportSchema.GenerateMigrationScript(options);

        // act
        await fixture.ApplyScriptAsync(scripted.DatabaseName, script, CancellationToken);
        await fixture.ApplyScriptAsync(scripted.DatabaseName, script, CancellationToken);
        await using var scriptedConnection = await OpenAsync(scripted.ConnectionString);
        await PostgresTransportSchema.MigrateAsync(scriptedConnection, options, CancellationToken);
        await fixture.ApplyScriptAsync(direct.DatabaseName, script, CancellationToken);

        // assert
        Assert.Equal(await ReadSchemaAsync(connection), await ReadSchemaAsync(scriptedConnection));
        Assert.Equal(await ReadHistoryAsync(connection), await ReadHistoryAsync(scriptedConnection));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_UpgradeAndPreserveData_When_InitialMigrationAlreadyApplied(bool script)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();
        await ExecuteAsync(connection, PostgresSchemaSql.InitialSchema(options));
        await ExecuteAsync(connection,
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
        if (script)
        {
            await fixture.ApplyScriptAsync(database.DatabaseName,
                PostgresTransportSchema.GenerateMigrationScript(options), CancellationToken);
        }
        else
        {
            await PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);
        }
        await using var freshConnection = await OpenAsync(database.ConnectionString);
        await ApplyAsync(freshConnection, options, !script);

        // assert
        (await ReadHistoryAsync(connection)).MatchInlineSnapshot(
            """
            [
              "2026-03-06_AddConsumerManagement",
              "2026-03-06_AddTransportIndex",
              "2026-03-06_InitialSchema",
              "future-migration"
            ]
            """);
        Assert.Equal("existing-queue:0102", await ScalarAsync(connection,
            "SELECT q.name || ':' || encode(m.body, 'hex') FROM public.mocha_message m JOIN public.mocha_queue q ON q.id = m.queue_id"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task MigrateAsync_Should_SerializeCallers_When_MigrationsRunConcurrently(int mode)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        var options = new PostgresSchemaOptions();

        // act
        await Task.WhenAll(Enumerable.Range(0, 4).Select(async index =>
        {
            await using var connection = await OpenAsync(database.ConnectionString);
            await ApplyAsync(connection, options, mode == 1 || (mode == 2 && index % 2 == 0));
        }));

        // assert
        await using var verification = await OpenAsync(database.ConnectionString);
        Assert.Equal(3, (await ReadHistoryAsync(verification)).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_RollBackAndAllowRetry_When_MigrationFails(bool script)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);
        await ExecuteAsync(connection, "CREATE VIEW public.mocha_message AS SELECT 1 AS sentinel;");

        // act
        await Assert.ThrowsAsync<PostgresException>(
            () => ApplyAsync(connection, new PostgresSchemaOptions(), script));
        if (script)
        {
            await ExecuteAsync(connection, "ROLLBACK;");
        }

        // assert
        Assert.Equal(0L, await ScalarAsync(connection,
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public';"));
        Assert.Equal(DBNull.Value, await ScalarAsync(connection,
            "SELECT to_regclass('public.mocha_topology_seq')::text;"));
        await ExecuteAsync(connection, "DROP VIEW public.mocha_message;");
        await ApplyAsync(connection, new PostgresSchemaOptions(), script);
        Assert.Equal(3, (await ReadHistoryAsync(connection)).Length);
    }

    [Fact]
    public async Task MigrateAsync_Should_CancelAndAllowRetry_When_WaitingForExistingMigrationLock()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var blocker = await OpenAsync(database.ConnectionString);
        await using var connection = await OpenAsync(database.ConnectionString);
        await using var transaction = await blocker.BeginTransactionAsync(CancellationToken);
        await ExecuteAsync(blocker, "SELECT pg_advisory_xact_lock(958913715);");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

        // act
        var migration = PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), cancellation.Token);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!(bool)(await ScalarAsync(blocker,
            $"SELECT EXISTS (SELECT FROM pg_locks WHERE pid = {connection.ProcessID} AND locktype = 'advisory' AND NOT granted);"))!)
        {
            await Task.Delay(20, timeout.Token);
        }
        await cancellation.CancelAsync();

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => migration);
        Assert.Equal(DBNull.Value, await ScalarAsync(connection, "SELECT to_regclass('public.mocha_migrations')::text;"));
        await transaction.RollbackAsync(CancellationToken);
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);
        Assert.Equal(3, (await ReadHistoryAsync(connection)).Length);
    }

    [Theory]
    [InlineData("messaging", "bus_")]
    [InlineData("MixedCase", "Bus_")]
    [InlineData("\"odd.schema'\\\"\"$mocha_migration$\"", "bus_")]
    public async Task MigrateAsync_Should_UseRuntimeNames_When_CustomNamesConfigured(string schema, string prefix)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions { Schema = schema, TablePrefix = prefix };

        // act
        await ApplyAsync(connection, options, true);
        await ApplyAsync(connection, options, false);

        // assert
        Assert.Equal(3L, await ScalarAsync(connection, $"SELECT count(*) FROM {options.MigrationsTable};"));
        Assert.Equal(0L, await ScalarAsync(connection, $"SELECT count(*) FROM {options.QueueTable};"));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_RejectTransaction_When_CallerOwnsTransaction(bool ambient)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);
        using var scope = ambient ? new TransactionScope(TransactionScopeAsyncFlowOption.Enabled) : null;
        await using var transaction = ambient ? null : await connection.BeginTransactionAsync(CancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken));

        // assert
        Assert.Equal("Transport migration requires a connection without an active or ambient transaction.", exception.Message);
        Assert.Equal(DBNull.Value, await ScalarAsync(connection, "SELECT to_regclass('public.mocha_migrations')::text;"));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT 1;"));
    }

    [Fact]
    public async Task MigrateAsync_Should_PreserveHistory_When_DatabaseClonedFromTemplate()
    {
        // arrange
        await using var template = await fixture.CreateDatabaseAsync("MigrationTemplate");
        await using var clone = await fixture.CreateDatabaseAsync("MigrationClone");
        await using (var source = await OpenAsync(template.ConnectionString))
        {
            await PostgresTransportSchema.MigrateAsync(source, new PostgresSchemaOptions(), CancellationToken);
        }
        await using var admin = await OpenAsync(fixture.ConnectionString);

        // act
        await ExecuteAsync(admin, $"DROP DATABASE \"{clone.DatabaseName}\";");
        await ExecuteAsync(admin, $"CREATE DATABASE \"{clone.DatabaseName}\" TEMPLATE \"{template.DatabaseName}\";");
        await using var connection = await OpenAsync(clone.ConnectionString);

        // assert
        Assert.Equal(3, (await ReadHistoryAsync(connection)).Length);
        Assert.Equal(0L, await ScalarAsync(connection, "SELECT count(*) FROM public.mocha_consumers;"));
        Assert.Equal(0L, await ScalarAsync(connection, "SELECT count(*) FROM public.mocha_queue;"));
        await AutoMigrateIntegrationTests.PublishAndReceiveAsync(clone.ConnectionString);
    }

    [Fact]
    public async Task MigrateAsync_Should_LeaveDatabaseUnchanged_When_CancelledBeforeExecution()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), cancellation.Token));

        // assert
        Assert.Equal(0L, await ScalarAsync(connection, "SELECT count(*) FROM pg_tables WHERE schemaname = 'public';"));
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateAsync_Should_RejectReplay_When_HistoryWasErased(bool script)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await OpenAsync(database.ConnectionString);
        var options = new PostgresSchemaOptions();
        await PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);
        await ExecuteAsync(connection, "TRUNCATE public.mocha_migrations;");

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ApplyAsync(connection, options, script));
        if (script)
        {
            await ExecuteAsync(connection, "ROLLBACK;");
        }

        // assert
        Assert.Equal(PostgresErrorCodes.DuplicateTable, exception.SqlState);
        Assert.Equal(0L, await ScalarAsync(connection, "SELECT count(*) FROM public.mocha_migrations;"));
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false, Enlist = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        return connection;
    }

    private static Task ApplyAsync(NpgsqlConnection connection, PostgresSchemaOptions options, bool script)
        => script
            ? ExecuteAsync(connection, PostgresTransportSchema.GenerateMigrationScript(options))
            : PostgresTransportSchema.MigrateAsync(connection, options, CancellationToken);

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(CancellationToken);
    }

    private static async Task<string[]> ReadHistoryAsync(NpgsqlConnection connection)
        => (string[])(await ScalarAsync(connection,
            "SELECT array_agg(migration_id ORDER BY migration_id) FROM public.mocha_migrations;"))!;

    private static async Task<string[]> ReadSchemaAsync(NpgsqlConnection connection)
        => (string[])(await ScalarAsync(connection,
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
            """))!;
}
