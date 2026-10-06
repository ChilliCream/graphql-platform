using System.Collections.Concurrent;
using System.Diagnostics;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class AutoMigrateIntegrationTests(PostgresFixture fixture)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(null, false)]
    [InlineData(true, false)]
    [InlineData(null, true)]
    [InlineData(true, true)]
    public async Task StartAsync_Should_Migrate_When_AutoMigrateEnabled(bool? autoMigrate, bool upgrade)
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        if (upgrade)
        {
            await ExecuteAsync(connection, PostgresSchemaSql.InitialSchema(new PostgresSchemaOptions()));
            await ExecuteAsync(connection,
                """
                CREATE TABLE public.mocha_migrations (
                    migration_id text PRIMARY KEY,
                    applied_on timestamptz NOT NULL DEFAULT now());
                INSERT INTO public.mocha_migrations (migration_id) VALUES ('2026-03-06_InitialSchema');
                """);
        }

        var services = new ServiceCollection();
        services.AddMessageBus().AddPostgres(t =>
        {
            t.ConnectionString(database.ConnectionString).AutoProvision(false);
            if (autoMigrate.HasValue)
            {
                t.AutoMigrate(autoMigrate.Value);
            }
        });
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        await runtime.StartAsync(CancellationToken);
        try
        {
            // assert
            Assert.Equal(3L, await ScalarAsync(connection, "SELECT count(*) FROM public.mocha_migrations;"));
            Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM public.mocha_consumers;"));
        }
        finally
        {
            await transport.StopAsync(runtime, CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_Should_SkipSchemaWork_When_PrivilegedRuntimeDisablesMigration()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);
        await ExecuteAsync(connection,
            """
            ALTER TABLE public.mocha_migrations RENAME TO saved_migrations;
            CREATE FUNCTION public.reject_history_read() RETURNS text AS $$
            BEGIN
                RAISE EXCEPTION 'Migration history must not be read';
            END;
            $$ LANGUAGE plpgsql;
            CREATE VIEW public.mocha_migrations AS
                SELECT public.reject_history_read() AS migration_id;
            CREATE FUNCTION public.reject_ddl() RETURNS event_trigger AS $$
            BEGIN
                RAISE EXCEPTION 'Schema DDL must not run';
            END;
            $$ LANGUAGE plpgsql;
            CREATE EVENT TRIGGER reject_ddl ON ddl_command_start EXECUTE FUNCTION public.reject_ddl();
            """);
        var statements = new ConcurrentBag<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Npgsql",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.GetTagItem("db.statement") is string sql)
                {
                    statements.Add(sql);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        // act
        await PublishAndReceiveAsync(database.ConnectionString);
        listener.Dispose();

        // assert
        Assert.Contains(statements, sql => sql.Contains("INSERT INTO public.mocha_consumers", StringComparison.Ordinal));
        statements.Where(sql =>
            sql.Contains("mocha_migrations", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("pg_catalog", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("information_schema", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("pg_tables", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("to_regclass", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("pg_advisory", StringComparison.OrdinalIgnoreCase))
            .ToArray().MatchInlineSnapshot("[]");
        Assert.Equal(3L, await ScalarAsync(connection, "SELECT count(*) FROM public.saved_migrations;"));
        Assert.True((long)(await ScalarAsync(connection, "SELECT count(*) FROM public.mocha_queue;"))! > 0);
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_RuntimeHasOnlyDmlPermissions()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await PostgresTransportSchema.MigrateAsync(connection, new PostgresSchemaOptions(), CancellationToken);
        var role = "mocha_runtime_" + Guid.NewGuid().ToString("N");
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

            // act
            await PublishAndReceiveAsync(settings.ConnectionString);

            // assert
            Assert.Equal(false, await ScalarAsync(connection,
                $"SELECT has_schema_privilege('{role}', 'public', 'CREATE');"));
            Assert.Equal(false, await ScalarAsync(connection,
                $"SELECT has_table_privilege('{role}', 'public.mocha_migrations', 'SELECT');"));
        }
        finally
        {
            await ExecuteAsync(connection, $"DROP OWNED BY {role}; DROP ROLE {role};");
        }
    }

    [Fact]
    public async Task StartAsync_Should_FailThroughConsumerRegistration_When_MigrationsDisabledOnEmptyDatabase()
    {
        // arrange
        await using var database = await fixture.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddMessageBus().AddPostgres(t => t.ConnectionString(database.ConnectionString).AutoMigrate(false));
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(() => runtime.StartAsync(CancellationToken).AsTask());

        // assert
        Assert.Equal(PostgresErrorCodes.UndefinedTable, exception.SqlState);
        Assert.Equal("relation \"public.mocha_consumers\" does not exist", exception.MessageText);
        await using var connection = CreateSetupConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        Assert.Equal(0L, await ScalarAsync(connection, "SELECT count(*) FROM pg_tables WHERE schemaname = 'public';"));
    }

    internal static async Task PublishAndReceiveAsync(string connectionString)
    {
        var recorder = new MessageRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder).AddMessageBus().AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t => t.ConnectionString(connectionString).AutoMigrate(false).AutoProvision(true));
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await using var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        await runtime.StartAsync(CancellationToken);
        try
        {
            using var scope = provider.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new OrderCreated { OrderId = "prepared-database" }, CancellationToken);
            Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(30)), "Handler did not receive the event.");
            Assert.Equal("prepared-database", Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages)).OrderId);
        }
        finally
        {
            await transport.StopAsync(runtime, CancellationToken.None);
        }
    }

    private static NpgsqlConnection CreateSetupConnection(string connectionString)
        => new(new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString);

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
}
