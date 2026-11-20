using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Stages message envelopes as entities of the DbContext that owns it.
/// </summary>
internal interface IDbContextOutboxSink
{
    /// <summary>
    /// Adds the envelope to the context as an entity that the next save inserts.
    /// </summary>
    /// <param name="envelope">The message envelope to stage.</param>
    /// <returns>The entry of the added entity.</returns>
    EntityEntry Stage(MessageEnvelope envelope);
}
