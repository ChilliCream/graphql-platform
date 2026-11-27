using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Signals the outbox processor of a context after a save that wrote outbox messages outside a database
/// transaction.
/// </summary>
internal sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        return new(SavedChanges(eventData, result));
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not { } context || context.Database.CurrentTransaction is not null)
        {
            return result;
        }

        var state = context.GetService<DbContextOutboxState>();

        if (state.HasUncommittedMessages)
        {
            state.HasUncommittedMessages = false;
            context.GetMessagingServices().OutboxSignal?.SetAfterAmbientTransaction();
        }

        return result;
    }
}
