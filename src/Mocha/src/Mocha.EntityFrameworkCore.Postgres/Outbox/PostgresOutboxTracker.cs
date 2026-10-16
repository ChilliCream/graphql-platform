using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.Outbox;

/// <summary>
/// Tracks outbox messages prepared for a DbContext save.
/// </summary>
internal sealed class PostgresOutboxTracker : SaveChangesInterceptor, IResettableService, IDisposable
{
    private readonly List<EntityEntry<OutboxMessage>> _entries = [];

    public void Track(DbContext context, OutboxMessage message)
    {
        try
        {
            _entries.Add(context.Add(message));
        }
        catch
        {
            message.Envelope.Dispose();
            throw;
        }
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.State == EntityState.Added)
            {
                // Entries cached before ChangeTracker.Clear can retain their previous state.
                entry = eventData.Context!.Entry(entry.Entity);
            }

            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                entry.State = EntityState.Detached;
                entry.Entity.Envelope.Dispose();
                _entries.RemoveAt(i);
            }
        }

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

    private void ReleaseMessages()
    {
        foreach (var entry in _entries)
        {
            entry.Entity.Envelope.Dispose();
        }

        _entries.Clear();
    }
}
