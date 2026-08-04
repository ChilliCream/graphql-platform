using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core save changes events to signal the outbox processor
/// that messages are ready for dispatch.
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
            signal.Set();
        }

        return result;
    }
}
