using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core save changes events to signal the scheduler
/// that messages are ready for dispatch.
/// </summary>
internal sealed class SchedulingSaveChangesInterceptor(ISchedulerSignal signal, TimeProvider timeProvider) : SaveChangesInterceptor, ISingletonInterceptor
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
            signal.Notify(timeProvider.GetUtcNow());
        }

        return result;
    }
}
