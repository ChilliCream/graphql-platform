using Microsoft.EntityFrameworkCore;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for persistence by the owning DbContext's next save.
/// </summary>
public interface IDbContextOutboxProvider
{
    /// <summary>
    /// Serializes and attaches an envelope to the specified context for its next save.
    /// On failure, the context must retain no changes from this operation.
    /// </summary>
    void Stage(DbContext context, MessageEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a marker for the messages currently staged by this provider.
    /// </summary>
    int CreateCheckpoint();

    /// <summary>
    /// Detaches and releases messages staged after the specified marker.
    /// </summary>
    void Discard(DbContext context, int checkpoint);
}
