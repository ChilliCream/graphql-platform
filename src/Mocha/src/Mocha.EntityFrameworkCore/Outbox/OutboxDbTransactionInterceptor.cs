using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core database transaction commit events to signal the outbox processor
/// that messages are ready for dispatch.
/// </summary>
internal sealed class OutboxDbTransactionInterceptor(IOutboxSignal signal)
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
        signal.Set();
    }
}
