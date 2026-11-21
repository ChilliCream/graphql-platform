using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mocha.Outbox;

/// <summary>
/// Rejects a save that would write added outbox messages outside a transaction.
/// </summary>
internal sealed class OutboxAtomicSaveInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureAtomicSave(eventData.Context);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureAtomicSave(eventData.Context);
        return new(result);
    }

    private static void EnsureAtomicSave(DbContext? context)
    {
        if (context is null || PostgresDbContextOutbox.IsSaveAtomic(context))
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<OutboxMessage>())
        {
            if (entry.State == EntityState.Added)
            {
                throw ThrowHelper.OutboxRequiresAtomicSave(context.GetType());
            }
        }
    }
}
