using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core save changes events to signal the scheduler
/// of the application that created the context.
/// </summary>
internal sealed class SchedulingSaveChangesInterceptor : SaveChangesInterceptor
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
            var services = context.GetMessagingServices();
            services.SchedulerSignal?.Notify(services.TimeProvider.GetUtcNow());
        }

        return result;
    }
}
