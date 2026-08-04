using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core database transaction commit events to signal the scheduler
/// that messages are ready for dispatch.
/// </summary>
internal sealed class SchedulingDbTransactionInterceptor(ISchedulerSignal signal, TimeProvider timeProvider)
    : DbTransactionInterceptor
    , ISingletonInterceptor
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
        signal.Notify(timeProvider.GetUtcNow());
    }
}
