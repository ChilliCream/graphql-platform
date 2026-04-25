using Microsoft.Extensions.Primitives;

namespace Mocha.Resources;

/// <summary>
/// A source of <see cref="MochaResource"/> instances with a change-token surface for live updates.
/// </summary>
/// <remarks>
/// Consumers must call <see cref="GetChangeToken"/> before reading <see cref="Resources"/>;
/// reading the snapshot first can pair a stale snapshot with a fresh token.
/// </remarks>
public abstract class MochaResourceSource
{
    /// <summary>
    /// Gets the current snapshot of resources contributed by this source.
    /// </summary>
    public abstract IReadOnlyList<MochaResource> Resources { get; }

    /// <summary>
    /// Gets a change token that fires when this source's resources change.
    /// </summary>
    /// <remarks>
    /// Tokens are single-use; callers must re-invoke this method after a token fires
    /// to obtain a fresh one.
    /// </remarks>
    public abstract IChangeToken GetChangeToken();
}
