using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Outbox;

/// <summary>
/// Implements <see cref="IMessageOutbox"/> for SQL Server by inserting serialized message envelopes
/// into the outbox table using raw SQL through the DbContext SqlConnection.
/// </summary>
/// <remarks>
/// When a database transaction is active on the DbContext, the insert participates in that transaction
/// and the outbox signal is deferred until commit. Without an active transaction, the signal fires
/// immediately after insert to wake the outbox processor.
/// </remarks>
internal sealed class SqlServerMessageOutbox : IMessageOutbox, IDisposable
{
    private readonly DbContext _originalDbContext;
    private readonly IOutboxSignal _signal;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private PooledArrayWriter? _arrayWriter;
    private readonly string? _insertSql;

    /// <summary>
    /// Creates a new <see cref="SqlServerMessageOutbox"/> using the provided DbContext connection,
    /// outbox signal, and pre-built insert SQL.
    /// </summary>
    /// <param name="originalDbContext">The DbContext whose underlying SQL Server connection is used for outbox inserts.</param>
    /// <param name="signal">The signal used to wake the outbox processor after a message is persisted.</param>
    /// <param name="insertSql">The parameterized SQL insert statement for the outbox table.</param>
    public SqlServerMessageOutbox(DbContext originalDbContext, IOutboxSignal signal, string insertSql)
    {
        _originalDbContext = originalDbContext;
        _signal = signal;
        _insertSql = insertSql;
    }

    /// <summary>
    /// Creates a new <see cref="SqlServerMessageOutbox"/> by resolving the DbContext, outbox signal,
    /// and named outbox options from the scoped service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="optionsName">The named options key used to retrieve <see cref="SqlServerMessageOutboxOptions"/>.</param>
    /// <param name="services">The scoped service provider used to resolve dependencies.</param>
    /// <returns>A new <see cref="SqlServerMessageOutbox"/> configured for the specified DbContext.</returns>
    public static SqlServerMessageOutbox Create(Type contextType, string optionsName, IServiceProvider services)
    {
        var dbContext = (DbContext)services.GetRequiredService(contextType);
        var signal = services.GetRequiredService<IOutboxSignal>();
        var outboxOptionsMonitor = services.GetRequiredService<IOptionsMonitor<SqlServerMessageOutboxOptions>>();
        var outboxOptions = outboxOptionsMonitor.Get(optionsName);
        var insertSql = outboxOptions.Queries.InsertEnvelope;

        return new SqlServerMessageOutbox(dbContext, signal, insertSql);
    }

    /// <summary>
    /// Serializes the message envelope and inserts it into the SQL Server outbox table.
    /// </summary>
    /// <remarks>
    /// If no database transaction is active, the outbox signal is raised immediately to wake the
    /// processor. Otherwise, the signal is deferred to the transaction commit interceptor.
    /// </remarks>
    /// <param name="envelope">The message envelope to persist in the outbox.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _arrayWriter ??= new PooledArrayWriter();

            var connection = (SqlConnection)_originalDbContext.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var transaction = _originalDbContext.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction;

            await using var writer = new Utf8JsonWriter(_arrayWriter);
            writer.WriteEnvelope(envelope);
            writer.Flush(); // we know it's not async

            var envelopeString = Encoding.UTF8.GetString(_arrayWriter.WrittenMemory.Span);

            // Execute the INSERT command
            await using var command = connection.CreateCommand();
            command.CommandText = _insertSql;
            command.Transaction = transaction;
            command.Parameters.AddWithValue("@id", NewVersion());
            command.Parameters.AddWithValue("@envelope", envelopeString);

            await command.PrepareAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);

            if (transaction is null)
            {
                _signal.Set();
            }
        }
        finally
        {
            _arrayWriter?.Reset();
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
