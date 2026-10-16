using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Signals the outbox processor after saves.
/// </summary>
internal sealed class OutboxSaveChangesInterceptor(IOutboxSignal signal) : SaveChangesInterceptor, ISingletonInterceptor
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
        if (eventData.Context is not { } context)
        {
            return result;
        }

        if (context.Database.CurrentTransaction is null)
        {
            signal.SetAfterAmbientTransaction();
        }

        return result;
    }
}
