using System.Data.Common;
using HotChocolate.Execution.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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

        await using var context = new BatchDbContext(CreateOptions(connectionString, capturedSql: null));
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await seed(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return connectionString;
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

    private static DbContextOptions<BatchDbContext> CreateOptions(string connectionString, List<string>? capturedSql)
    {
        var builder = new DbContextOptionsBuilder<BatchDbContext>().UseNpgsql(connectionString);

        if (capturedSql is not null)
        {
            builder.AddInterceptors(new SqlCaptureInterceptor(capturedSql));
        }

        return builder.Options;
    }

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
