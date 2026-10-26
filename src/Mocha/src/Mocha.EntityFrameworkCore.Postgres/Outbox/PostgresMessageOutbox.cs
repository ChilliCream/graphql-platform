using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore;
using Mocha.Middlewares;
using Mocha.Utils;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.Outbox;

/// <summary>
/// Implements <see cref="IMessageOutbox"/> for Postgres by storing serialized message envelopes
/// in the outbox table.
/// </summary>
/// <remarks>
/// Direct publishes participate in the DbContext transaction or ambient <see cref="System.Transactions.Transaction"/>
/// and signal after commit. A direct publish or send without a transaction throws when a transaction is required,
/// and otherwise signals immediately after insertion.
/// Messages prepared with <see cref="DbContextOutbox"/> are persisted and signaled by its save.
/// </remarks>
internal sealed class PostgresMessageOutbox : IMessageOutbox, IDisposable
{
    private readonly DbContext _dbContext;
    private readonly IOutboxSignal _signal;
    private readonly bool _requireTransaction;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private PooledArrayWriter? _arrayWriter;
    private readonly string? _insertSql;

    /// <summary>
    /// Creates a new <see cref="PostgresMessageOutbox"/> using the provided DbContext connection,
    /// outbox signal, and pre-built insert SQL.
    /// </summary>
    /// <param name="dbContext">The DbContext whose connection is used for outbox inserts.</param>
    /// <param name="signal">The signal used to wake the outbox processor after a message is persisted.</param>
    /// <param name="insertSql">The parameterized SQL insert statement for the outbox table.</param>
    /// <param name="requireTransaction">Whether a direct publish or send without an active transaction throws.</param>
    public PostgresMessageOutbox(
        DbContext dbContext,
        IOutboxSignal signal,
        string insertSql,
        bool requireTransaction)
    {
        _dbContext = dbContext;
        _signal = signal;
        _insertSql = insertSql;
        _requireTransaction = requireTransaction;
    }

    /// <summary>
    /// Creates a new <see cref="PostgresMessageOutbox"/> by resolving the DbContext, outbox signal,
    /// and named outbox options from the scoped service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="optionsName">The named options key used to retrieve <see cref="PostgresMessageOutboxOptions"/>.</param>
    /// <param name="services">The scoped service provider used to resolve dependencies.</param>
    /// <returns>A new <see cref="PostgresMessageOutbox"/> configured for the specified DbContext.</returns>
    public static PostgresMessageOutbox Create(Type contextType, string optionsName, IServiceProvider services)
    {
        var dbContext = (DbContext)services.GetRequiredService(contextType);
        var signal = services.GetRequiredService<IOutboxSignal>();
        var outboxOptionsMonitor = services.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>();
        var outboxOptions = outboxOptionsMonitor.Get(optionsName);
        var insertSql = outboxOptions.Queries.InsertEnvelope;
        var options = services.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>().Get(optionsName);

        return new PostgresMessageOutbox(dbContext, signal, insertSql, options.RequireTransaction);
    }

    /// <summary>
    /// Serializes the message envelope and stores it in the Postgres outbox table.
    /// </summary>
    /// <remarks>
    /// With an active database transaction, the signal is deferred to the transaction commit interceptor.
    /// With an ambient <see cref="System.Transactions.Transaction"/>, it is raised when that transaction completes.
    /// Otherwise, it is raised immediately.
    /// </remarks>
    /// <param name="envelope">The message envelope to persist in the outbox.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <exception cref="InvalidOperationException">
    /// A transaction is required and a publish or send is persisted without one.
    /// </exception>
    public async ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        if (_requireTransaction && RequiresTransaction(envelope) && !HasTransaction())
        {
            throw Mocha.EntityFrameworkCore.Postgres.ThrowHelper.OutboxRequiresTransaction(_dbContext.GetType());
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _arrayWriter ??= new PooledArrayWriter();

            var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();

            try
            {
                await _dbContext.Database.OpenConnectionAsync(cancellationToken);

                await using var writer = new Utf8JsonWriter(_arrayWriter);
                writer.WriteEnvelope(envelope);
                writer.Flush(); // we know it's not async

                // Execute the INSERT command
                await using var command = connection.CreateCommand();
                command.CommandText = _insertSql;
                command.Parameters.AddWithValue("@id", NewVersion());
                command.Parameters.Add(
                    new NpgsqlParameter("@envelope", NpgsqlDbType.Json) { Value = _arrayWriter.WrittenMemory });
                await command.PrepareAsync(cancellationToken);

                await command.ExecuteNonQueryAsync(cancellationToken);

                if (_dbContext.Database.CurrentTransaction?.GetDbTransaction() is not NpgsqlTransaction)
                {
                    _signal.SetAfterAmbientTransaction();
                }
            }
            finally
            {
                await _dbContext.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            _arrayWriter?.Reset();
            _semaphore.Release();
        }
    }

    private bool HasTransaction()
        => Transaction.Current is not null || _dbContext.Database.CurrentTransaction is not null;

    // Replies and faults can be dispatched after the handler transaction rolls back.
    private static bool RequiresTransaction(MessageEnvelope envelope)
        => envelope.Headers?.Get(MessageHeaders.MessageKind) is MessageKind.Publish or MessageKind.Send;

    private static Guid NewVersion()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Guid.NewGuid();
#endif
    }

    /// <summary>
    /// Releases the semaphore and pooled array writer used for outbox message serialization.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
        _arrayWriter?.Dispose();
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
