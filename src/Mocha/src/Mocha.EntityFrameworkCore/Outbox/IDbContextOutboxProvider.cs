using Microsoft.EntityFrameworkCore;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for persistence by the owning DbContext's next save.
/// </summary>
public interface IDbContextOutboxProvider
{
    /// <summary>
    /// Validates that the context can stage messages for a registered relay destination.
    /// </summary>
    void Validate(DbContext context);

    /// <summary>
    /// Creates an independent snapshot of an envelope without writing to the database or change tracker.
    /// </summary>
    IPreparedOutboxMessage Prepare(MessageEnvelope envelope);

    /// <summary>
    /// Attaches a prepared message to the specified context and transfers ownership on success.
    /// On failure, the context must retain no changes from this operation.
    /// </summary>
    void Stage(DbContext context, IPreparedOutboxMessage message);
}
