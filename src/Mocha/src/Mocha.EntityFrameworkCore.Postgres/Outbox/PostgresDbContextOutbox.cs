using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Outbox;

internal sealed class PostgresDbContextOutbox(TimeProvider timeProvider, ICurrentDbContext currentContext)
    : SaveChangesInterceptor, IMessageOutbox, IResettableService, IDisposable
{
    private readonly List<EntityEntry<OutboxMessage>> _entries = [];
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;

    public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
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
            Track(
                currentContext.Context,
                new OutboxMessage(OutboxMessageIdGenerator.Next(), document, timeProvider.GetUtcNow().UtcDateTime));
            document = null;
        }
        finally
        {
            document?.Dispose();
            _jsonWriter.Reset();
            _arrayWriter.Reset();
        }

        return default;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context!;
        PruneMessages(context);
        if (_entries.Count > 0)
        {
            EnsureAtomicSave(context);
        }

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
        => new(SavingChanges(eventData, result));

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        PruneMessages(eventData.Context!);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
        => new(SavedChanges(eventData, result));

    void IResettableService.ResetState() => ReleaseMessages();

    Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
    {
        ReleaseMessages();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        ReleaseMessages();
        _jsonWriter?.Dispose();
        _arrayWriter?.Dispose();
    }

    private void Track(DbContext context, OutboxMessage message)
    {
        EnsureAtomicSave(context);
        try
        {
            _entries.Add(context.Add(message));
        }
        catch
        {
            context.Entry(message).State = EntityState.Detached;
            throw;
        }
    }

    private static void EnsureAtomicSave(DbContext context)
    {
        if (context.Database.AutoTransactionBehavior == AutoTransactionBehavior.Never
            && context.Database.CurrentTransaction is null
            && Transaction.Current is null)
        {
            throw ThrowHelper.OutboxRequiresAtomicSave(context.GetType());
        }
    }

    private void PruneMessages(DbContext context)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.State == EntityState.Added)
            {
                // Entries cached before ChangeTracker.Clear can retain their previous state.
                entry = context.Entry(entry.Entity);
            }

            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                entry.State = EntityState.Detached;
                entry.Entity.Dispose();
                _entries.RemoveAt(i);
            }
        }
    }

    private void ReleaseMessages()
    {
        foreach (var entry in _entries)
        {
            entry.Entity.Dispose();
        }

        _entries.Clear();
    }
}
