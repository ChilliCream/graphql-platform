using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
/// and signal after commit; without a transaction, they signal immediately after insertion.
/// Messages prepared with <see cref="DbContextOutbox"/> are persisted and signaled by its save.
/// </remarks>
internal sealed class PostgresMessageOutbox : IMessageOutbox, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly Type _contextType;
    private DbContext? _originalDbContext;
    private readonly IOutboxSignal _signal;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;
    private readonly string? _insertSql;

    /// <summary>
    /// Creates a new <see cref="PostgresMessageOutbox"/> using the provided DbContext connection,
    /// outbox signal, and pre-built insert SQL.
    /// </summary>
    /// <param name="services">The scoped service provider used to resolve the DbContext.</param>
    /// <param name="contextType">The DbContext type whose connection is used for outbox inserts.</param>
    /// <param name="signal">The signal used to wake the outbox processor after a message is persisted.</param>
    /// <param name="insertSql">The parameterized SQL insert statement for the outbox table.</param>
    public PostgresMessageOutbox(
        IServiceProvider services,
        Type contextType,
        IOutboxSignal signal,
        string insertSql)
    {
        _services = services;
        _contextType = contextType;
        _signal = signal;
        _insertSql = insertSql;
    }

    /// <summary>
    /// Creates a new <see cref="PostgresMessageOutbox"/> by resolving the outbox signal
    /// and named outbox options from the scoped service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="optionsName">The named options key used to retrieve <see cref="PostgresMessageOutboxOptions"/>.</param>
    /// <param name="services">The scoped service provider used to resolve dependencies.</param>
    /// <returns>A new <see cref="PostgresMessageOutbox"/> configured for the specified DbContext.</returns>
    public static PostgresMessageOutbox Create(Type contextType, string optionsName, IServiceProvider services)
    {
        var signal = services.GetRequiredService<IOutboxSignal>();
        var outboxOptionsMonitor = services.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>();
        var outboxOptions = outboxOptionsMonitor.Get(optionsName);
        var insertSql = outboxOptions.Queries.InsertEnvelope;

        return new PostgresMessageOutbox(services, contextType, signal, insertSql);
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
    public async ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var targetOutbox = OutboxPublishContext.Current;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _arrayWriter ??= new PooledArrayWriter();
            _jsonWriter ??= new Utf8JsonWriter(_arrayWriter);
            _jsonWriter.WriteEnvelope(envelope);
            _jsonWriter.Flush();

            if (targetOutbox is not null)
            {
                AddMessage(targetOutbox, _arrayWriter.WrittenMemory);
            }
            else
            {
                var dbContext = OriginalDbContext;
                var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();

                try
                {
                    await dbContext.Database.OpenConnectionAsync(cancellationToken);

                    await using var command = connection.CreateCommand();
                    command.CommandText = _insertSql;
                    command.Parameters.AddWithValue("@id", NewVersion());
                    command.Parameters.Add(
                        new NpgsqlParameter("@envelope", NpgsqlDbType.Json) { Value = _arrayWriter.WrittenMemory });
                    await command.PrepareAsync(cancellationToken);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    if (dbContext.Database.CurrentTransaction?.GetDbTransaction() is not NpgsqlTransaction)
                    {
                        _signal.SetAfterAmbientTransaction();
                    }
                }
                finally
                {
                    await dbContext.Database.CloseConnectionAsync();
                }
            }
        }
        finally
        {
            _jsonWriter?.Reset();
            _arrayWriter?.Reset();
            _semaphore.Release();
        }
    }

    private DbContext OriginalDbContext
        => _originalDbContext ??= _services.GetKeyedService<DbContextAccessor>(_contextType)?.Context
            ?? (DbContext)_services.GetRequiredService(_contextType);

    private static void AddMessage(DbContextOutbox outbox, ReadOnlyMemory<byte> envelope)
    {
        var tracker = outbox.Context.GetService<PostgresOutboxTracker>();
        var reader = new Utf8JsonReader(envelope.Span);
        var document = JsonDocument.ParseValue(ref reader);
        tracker.Track(outbox.Context, new OutboxMessage(NewVersion(), document));
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
        _jsonWriter?.Dispose();
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
