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

internal sealed class PostgresDbContextOutbox(ICurrentDbContext currentContext) : IDbContextOutboxSink, IDisposable
{
    private TimeProvider? _timeProvider;
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;

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

    public void Dispose()
    {
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
