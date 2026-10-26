using Microsoft.EntityFrameworkCore;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for persistence by the owning DbContext's next save.
/// </summary>
public interface IDbContextOutbox
{
    /// <summary>
    /// Stages an envelope for the specified context's next save.
    /// </summary>
    void Stage(DbContext context, MessageEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a marker for the messages currently staged by this outbox.
    /// </summary>
    int CreateCheckpoint();

    /// <summary>
    /// Discards messages staged after the specified checkpoint.
    /// </summary>
    void Discard(DbContext context, int checkpoint);
}
