using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core database transaction commit events to signal the scheduler
/// of the application that created the context.
/// </summary>
internal sealed class SchedulingDbTransactionInterceptor : DbTransactionInterceptor
{
    /// <inheritdoc />
    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        TransactionCommitted(transaction, eventData);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        if (eventData.Context?.GetApplicationServiceProvider() is { } services)
        {
            services.NotifyScheduler();
        }
    }
}
