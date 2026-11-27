using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Signals the outbox processor of a context when a transaction commits in which the context wrote
/// outbox messages.
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
        if (eventData.Context is not { } context)
        {
            return;
        }

        var state = context.GetService<DbContextOutboxState>();

        if (state.HasUncommittedMessages)
        {
            state.HasUncommittedMessages = false;
            context.GetMessagingServices().OutboxSignal?.Set();
        }
    }
}
