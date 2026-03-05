using Microsoft.EntityFrameworkCore.Storage;
using Mocha.Outbox;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Wraps an Entity Framework Core <see cref="IDbContextTransaction"/> as an <see cref="ISagaTransaction"/>
/// so that saga operations participate in the same database transaction as the DbContext.
/// </summary>
/// <param name="transaction">The underlying EF Core database transaction to wrap.</param>
public sealed class EfCoreSagaTransaction(IDbContextTransaction transaction) : ISagaTransaction
{
    /// <summary>
    /// Commits the underlying database transaction, persisting all saga state changes.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Rolls back the underlying database transaction, discarding all saga state changes.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the underlying database transaction and releases associated resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
    }
}
