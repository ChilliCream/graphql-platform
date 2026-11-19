using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Mocha.EntityFrameworkCore;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Outbox;

internal sealed class PostgresDbContextOutbox(ICurrentDbContext currentContext)
    : IMessageOutbox, IResettableService, IDisposable
{
    private readonly List<EntityEntry<OutboxMessage>> _entries = [];
    private EventHandler<SavingChangesEventArgs>? _onSavingChanges;
    private EventHandler<SavedChangesEventArgs>? _onSavedChanges;
    private TimeProvider? _timeProvider;
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;

    public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = currentContext.Context;
        _timeProvider ??= context.GetApplicationServiceProvider()!.GetTimeProvider();
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
                context,
                new OutboxMessage(OutboxMessageIdGenerator.Next(), document, _timeProvider.GetUtcNow().UtcDateTime));
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

        if (_entries.Count == 1)
        {
            Subscribe(context);
        }
    }

    private void OnSavingChanges(object? sender, SavingChangesEventArgs eventArgs)
    {
        var context = currentContext.Context;
        PruneMessages(context);
        if (_entries.Count > 0)
        {
            EnsureAtomicSave(context);
        }
    }

    private void OnSavedChanges(object? sender, SavedChangesEventArgs eventArgs)
        => PruneMessages(currentContext.Context);

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

        if (_entries.Count == 0)
        {
            Unsubscribe(context);
        }
    }

    private void ReleaseMessages()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        foreach (var entry in _entries)
        {
            entry.Entity.Dispose();
        }

        _entries.Clear();
        Unsubscribe(currentContext.Context);
    }

    private void Subscribe(DbContext context)
    {
        context.SavingChanges += _onSavingChanges ??= OnSavingChanges;
        context.SavedChanges += _onSavedChanges ??= OnSavedChanges;
    }

    private void Unsubscribe(DbContext context)
    {
        context.SavingChanges -= _onSavingChanges;
        context.SavedChanges -= _onSavedChanges;
    }
}
