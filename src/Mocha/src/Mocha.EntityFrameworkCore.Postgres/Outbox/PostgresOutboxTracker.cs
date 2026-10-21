using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Mocha.EntityFrameworkCore;

namespace Mocha.Outbox;

/// <summary>
/// Tracks outbox messages prepared for a DbContext save.
/// </summary>
internal sealed class PostgresOutboxTracker : SaveChangesInterceptor, IResettableService, IDisposable
{
    private readonly List<EntityEntry<OutboxMessage>> _entries = [];
    private Guid? _groupId;
    private long _sequence;

    public void Track(DbContext context, OutboxMessage message)
    {
        EnsureAtomicSave(context);
        if (message.ScheduledAt is null)
        {
            _groupId ??= message.Id;
            message.GroupId = _groupId;
            message.Sequence = _sequence++;
        }

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

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context!;
        PruneMessages(context);
        if (_entries.Count > 0)
        {
            context.GetService<IDbContextOutboxProvider>().Validate(context);
        }

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
        _groupId = null;
        _sequence = 0;
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

    void IDisposable.Dispose() => ReleaseMessages();

    private static void EnsureAtomicSave(DbContext context)
    {
        if (context.Database.AutoTransactionBehavior == AutoTransactionBehavior.Never
            && context.Database.CurrentTransaction is null
            && Transaction.Current is null)
        {
            throw Mocha.EntityFrameworkCore.Postgres.ThrowHelper.OutboxRequiresAtomicSave(context.GetType());
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
                _entries.RemoveAt(i);
            }
        }

        if (_entries.Count == 0)
        {
            _groupId = null;
            _sequence = 0;
        }
    }

    private void ReleaseMessages()
    {
        _entries.Clear();
        _groupId = null;
        _sequence = 0;
    }
}
