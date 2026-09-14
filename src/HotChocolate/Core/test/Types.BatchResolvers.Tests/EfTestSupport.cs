using System.Data.Common;
using HotChocolate.Execution.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Shared Postgres-backed EF Core plumbing for the filtering, sorting, and paging batch
/// families: each test gets its own isolated database seeded once, and the SQL text (never the
/// timing) issued against it can be captured for a snapshot.
/// </summary>
internal static class EfTestSupport
{
    /// <summary>
    /// Creates a fresh, isolated Postgres database, applies the shared schema, and hands it to
    /// <paramref name="seed"/> to populate before any query runs against it.
    /// </summary>
    public static async Task<string> CreateSeededDatabaseAsync(
        this PostgreSqlResource resource,
        Func<BatchDbContext, CancellationToken, Task> seed,
        CancellationToken cancellationToken)
    {
        var connectionString = resource.GetConnectionString($"batch_{Guid.NewGuid():N}");

        await using var context = new BatchDbContext(CreateOptions(connectionString));
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await seed(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return connectionString;
    }

    /// <summary>
    /// Drops the database <paramref name="connectionString"/> points at (as returned by
    /// <see cref="CreateSeededDatabaseAsync"/>), so a per-test Postgres database never outlives
    /// its test. Terminates any other backend still connected to it first, since Postgres refuses
    /// to drop a database with active connections.
    /// </summary>
    public static async Task DropDatabaseAsync(
        this PostgreSqlResource resource,
        string connectionString,
        CancellationToken cancellationToken)
    {
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database!;

        await using var connection = resource.GetConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText =
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity "
                + "WHERE datname = @name AND pid <> pg_backend_pid()";
            terminate.Parameters.AddWithValue("name", databaseName);
            await terminate.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
        await drop.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Registers <see cref="BatchDbContext"/> against the given connection string. When
    /// <paramref name="capturedSql"/> is supplied, every command executed through this context
    /// has its <see cref="DbCommand.CommandText"/> (parameterized, never the literal values or
    /// elapsed time) appended to it, ready to snapshot.
    /// </summary>
    public static IRequestExecutorBuilder AddBatchDbContext(
        this IRequestExecutorBuilder builder,
        string connectionString,
        List<string>? capturedSql = null)
    {
        builder.Services.AddDbContext<BatchDbContext>(
            (_, options) => options.UseNpgsql(connectionString).AddInterceptors(
                capturedSql is null ? [] : [new SqlCaptureInterceptor(capturedSql)]));

        return builder;
    }

    private static DbContextOptions<BatchDbContext> CreateOptions(string connectionString)
        => new DbContextOptionsBuilder<BatchDbContext>().UseNpgsql(connectionString).Options;

    /// <summary>
    /// Records the parameterized SQL text of every command HotChocolate issues through EF Core,
    /// deliberately excluding parameter values and elapsed time so the capture stays deterministic
    /// enough to snapshot.
    /// </summary>
    private sealed class SqlCaptureInterceptor(List<string> statements) : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            statements.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            statements.Add(command.CommandText);
            return new ValueTask<InterceptionResult<DbDataReader>>(result);
        }
    }
}
