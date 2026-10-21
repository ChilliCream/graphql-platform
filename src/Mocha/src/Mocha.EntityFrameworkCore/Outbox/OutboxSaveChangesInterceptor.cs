using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Signals the outbox processor after saves.
/// </summary>
internal sealed class OutboxSaveChangesInterceptor(IOutboxSignal signal) : SaveChangesInterceptor, ISingletonInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context?.GetInfrastructure().GetService<DbContextOutboxScope>()?.IsPreparing is true)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
        => new(SavingChanges(eventData, result));

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
