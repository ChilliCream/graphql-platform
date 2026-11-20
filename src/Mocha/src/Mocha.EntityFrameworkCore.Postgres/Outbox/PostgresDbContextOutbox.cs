using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Mocha.EntityFrameworkCore;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Outbox;

/// <summary>
/// Stages message envelopes as <see cref="OutboxMessage"/> entities of its context. The documents of the staged
/// envelopes are released when the context is disposed or returned to its pool.
/// </summary>
internal sealed class PostgresDbContextOutbox(ICurrentDbContext currentContext)
    : IDbContextOutboxSink, IResettableService, IDisposable
{
    private TimeProvider? _timeProvider;
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;
    private List<JsonDocument>? _documents;

    public EntityEntry Stage(MessageEnvelope envelope)
    {
        var context = currentContext.Context;
        EnsureAtomicSave(context);
        _timeProvider ??= context.GetMessagingServices().TimeProvider;
        _arrayWriter ??= new PooledArrayWriter();
        _jsonWriter ??= new Utf8JsonWriter(_arrayWriter);
        JsonDocument? document = null;

        try
        {
            var writer = new MessageEnvelopeWriter(_jsonWriter);
            writer.WriteMessage(envelope);
            _jsonWriter.Flush();
            var reader = new Utf8JsonReader(_arrayWriter.WrittenMemory.Span);
            document = JsonDocument.ParseValue(ref reader);
            var entry = Add(
                context,
                new OutboxMessage(OutboxMessageIdGenerator.Next(), document, _timeProvider.GetUtcNow().UtcDateTime));
            (_documents ??= []).Add(document);
            document = null;

            return entry;
        }
        finally
        {
            document?.Dispose();
            _jsonWriter.Reset();
            _arrayWriter.Reset();
        }
    }

    public void ResetState()
    {
        if (_documents is null)
        {
            return;
        }

        foreach (var document in _documents)
        {
            document.Dispose();
        }

        _documents.Clear();
    }

    public Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        ResetState();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        ResetState();
        _jsonWriter?.Dispose();
        _arrayWriter?.Dispose();
    }

    /// <summary>
    /// Returns <c>false</c> when automatic transactions are disabled and the connection of the context takes part
    /// in no transaction.
    /// </summary>
    internal static bool IsSaveAtomic(DbContext context)
        => context.Database.AutoTransactionBehavior != AutoTransactionBehavior.Never
            || context.Database.CurrentTransaction is not null
            || HasSystemTransaction(context);

    private static EntityEntry<OutboxMessage> Add(DbContext context, OutboxMessage message)
    {
        try
        {
            return context.Add(message);
        }
        catch (Exception ex) when (context.Model.FindEntityType(typeof(OutboxMessage)) is null)
        {
            throw ThrowHelper.OutboxMessageNotInModel(context.GetType(), ex);
        }
        catch
        {
            Detach(context, message);
            throw;
        }
    }

    private static void Detach(DbContext context, OutboxMessage message)
    {
        try
        {
            context.Entry(message).State = EntityState.Detached;
        }
        catch
        {
            // The caller rethrows the exception of the failed add.
        }
    }

    private static void EnsureAtomicSave(DbContext context)
    {
        if (!IsSaveAtomic(context))
        {
            throw ThrowHelper.OutboxRequiresAtomicSave(context.GetType());
        }
    }

    private static bool HasSystemTransaction(DbContext context)
        => context.GetService<IDbContextTransactionManager>() is ITransactionEnlistmentManager manager
            ? manager.EnlistedTransaction is not null || manager.CurrentAmbientTransaction is not null
            : Transaction.Current is not null;
}
