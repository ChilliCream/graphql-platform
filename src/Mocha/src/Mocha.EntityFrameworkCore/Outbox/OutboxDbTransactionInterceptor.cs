using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Intercepts Entity Framework Core database transaction commit events to signal the outbox processor
/// of the application that created the context.
/// </summary>
internal sealed class OutboxDbTransactionInterceptor : DbTransactionInterceptor
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
            services.GetRequiredService<IOutboxSignal>().Set();
        }
    }
}
