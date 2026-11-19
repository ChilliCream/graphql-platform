using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core save changes events to signal the outbox processor
/// of the application that created the context.
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
        if (eventData.Context is not { } context)
        {
            return result;
        }

        if (context.Database.CurrentTransaction is null
            && context.GetApplicationServiceProvider() is { } services)
        {
            services.GetRequiredService<IOutboxSignal>().SetAfterAmbientTransaction();
        }

        return result;
    }
}
