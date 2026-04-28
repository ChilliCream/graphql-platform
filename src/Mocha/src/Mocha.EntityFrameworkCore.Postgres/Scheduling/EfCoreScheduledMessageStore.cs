using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Middlewares;
using Mocha.Utils;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.Scheduling;

/// <summary>
/// Implements <see cref="IScheduledMessageStore"/> for Postgres by inserting serialized message envelopes
/// into the scheduled messages table using raw SQL through the DbContext Npgsql connection.
/// </summary>
/// <remarks>
/// Creates a new <see cref="EfCoreScheduledMessageStore"/> using the provided DbContext connection,
/// scheduler signal, and pre-built SQL statements.
/// </remarks>
/// <param name="originalDbContext">The DbContext whose underlying Npgsql connection is used for operations.</param>
/// <param name="signal">The signal used to wake the scheduler after a message is persisted.</param>
/// <param name="insertSql">The parameterized SQL insert statement for the scheduled messages table.</param>
/// <param name="cancelSql">The parameterized SQL delete statement for cancelling a scheduled message.</param>
internal sealed class EfCoreScheduledMessageStore(
    DbContext originalDbContext,
    ISchedulerSignal signal,
    string insertSql,
    string cancelSql) : IScheduledMessageStore, IDisposable
{
    private const string ProviderPrefix = "postgres-scheduler:";
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private PooledArrayWriter? _arrayWriter;

    /// <summary>
    /// Serializes the message envelope and inserts it into the Postgres scheduled messages table.
    /// </summary>
    /// <param name="context">The dispatch context whose envelope is to be persisted.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>An opaque token string for later cancellation.</returns>
    public async ValueTask<string> PersistAsync(
        IDispatchContext context,
        CancellationToken cancellationToken)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException(
                "EfCoreScheduledMessageStore.PersistAsync requires a serialized envelope on the dispatch context.");
        }

        if (envelope.ScheduledTime is not { } scheduledTime)
        {
            throw new InvalidOperationException(
                "EfCoreScheduledMessageStore.PersistAsync requires the envelope to carry a scheduled time.");
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            _arrayWriter ??= new PooledArrayWriter();

            var connection = (NpgsqlConnection)originalDbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var transaction = originalDbContext.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;

            await using var writer = new Utf8JsonWriter(_arrayWriter);
            writer.WriteEnvelope(envelope);
            writer.Flush(); // we know it's not async

            // Execute the INSERT command
            var id = NewVersion();
            await using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            if (transaction is not null)
            {
                command.Transaction = transaction;
            }
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.Add(
                new NpgsqlParameter("@envelope", NpgsqlDbType.Json) { Value = _arrayWriter.WrittenMemory });
            command.Parameters.AddWithValue("@scheduled_time", scheduledTime.UtcDateTime);
            await command.PrepareAsync(cancellationToken);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (transaction is null)
            {
                signal.Notify(scheduledTime);
            }

            return $"{ProviderPrefix}{id}";
        }
        finally
        {
            _arrayWriter?.Reset();
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Cancels a scheduled message by deleting it from the store.
    /// </summary>
    /// <param name="token">The opaque scheduling token returned by <see cref="PersistAsync"/>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns><c>true</c> if the message was cancelled; <c>false</c> if not found or already dispatched.</returns>
    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        var value = token.StartsWith(ProviderPrefix, StringComparison.Ordinal)
            ? token[ProviderPrefix.Length..]
            : token;

        if (!Guid.TryParse(value, out var id))
        {
            return false;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var connection = (NpgsqlConnection)originalDbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var transaction = originalDbContext.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;

            await using var command = connection.CreateCommand();
            command.CommandText = cancelSql;
            if (transaction is not null)
            {
                command.Transaction = transaction;
            }
            command.Parameters.AddWithValue("@id", id);
            await command.PrepareAsync(cancellationToken);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null and not DBNull;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static Guid NewVersion()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Guid.NewGuid();
#endif
    }

    /// <summary>
    /// Releases the semaphore and pooled array writer used for scheduled message serialization.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
        _arrayWriter?.Dispose();
    }

    /// <summary>
    /// Creates a new <see cref="EfCoreScheduledMessageStore"/> by resolving the DbContext, scheduler signal,
    /// and named options from the scoped service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="optionsName">The named options key used to retrieve <see cref="PostgresScheduledMessageOptions"/>.</param>
    /// <param name="services">The scoped service provider used to resolve dependencies.</param>
    /// <returns>A new <see cref="EfCoreScheduledMessageStore"/> configured for the specified DbContext.</returns>
    public static EfCoreScheduledMessageStore Create(Type contextType, string optionsName, IServiceProvider services)
    {
        var dbContext = (DbContext)services.GetRequiredService(contextType);
        var signal = services.GetRequiredService<ISchedulerSignal>();
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<PostgresScheduledMessageOptions>>();
        var options = optionsMonitor.Get(optionsName);
        var insertSql = options.Queries.InsertMessage;
        var cancelSql = options.Queries.CancelMessage;

        return new EfCoreScheduledMessageStore(dbContext, signal, insertSql, cancelSql);
    }
}

file static class Extensions
{
    public static void WriteEnvelope(this Utf8JsonWriter writer, MessageEnvelope envelope)
    {
        var envelopeWriter = new MessageEnvelopeWriter(writer);
        envelopeWriter.WriteMessage(envelope);
    }
}
