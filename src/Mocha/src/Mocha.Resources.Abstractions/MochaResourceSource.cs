using Microsoft.Extensions.Primitives;

namespace Mocha.Resources;

/// <summary>
/// A source of <see cref="MochaResource"/> instances with an ASP.NET Core–style
/// change-token surface for live updates.
/// </summary>
/// <remarks>
/// <para>
/// Modelled after <c>Microsoft.AspNetCore.Routing.EndpointDataSource</c>. Consumers
/// read a <see cref="GetChangeToken"/> first, then take a snapshot of
/// <see cref="Resources"/>, and re-read both when the token fires. Reading the snapshot
/// before the token risks observing a stale snapshot together with a fresh token.
/// </para>
/// <para>
/// Contributors derive this type for sources whose contents change (e.g. a router
/// that produces dispatch endpoints lazily). Use <c>StaticMochaResourceSource</c>
/// from <c>Mocha.Resources</c> for immutable snapshots.
/// </para>
/// </remarks>
public abstract class MochaResourceSource
{
    /// <summary>
    /// Gets the current snapshot of resources contributed by this source.
    /// </summary>
    /// <remarks>
    /// Implementations must return an immutable list so callers can iterate without locking.
    /// </remarks>
    public abstract IReadOnlyList<MochaResource> Resources { get; }

    /// <summary>
    /// Gets a change token that fires when this source's resources change.
    /// </summary>
    /// <remarks>
    /// Tokens are single-use; after a token fires, callers must re-invoke this method
    /// to obtain a fresh token. Implementations whose resources never change should
    /// return a non-firing token (see <c>StaticMochaResourceSource</c>).
    /// </remarks>
    public abstract IChangeToken GetChangeToken();
}
