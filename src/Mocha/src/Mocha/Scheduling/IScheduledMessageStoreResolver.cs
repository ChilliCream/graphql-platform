using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Selects a single <see cref="IScheduledMessageStore"/> for a scheduled dispatch operation
/// (by <see cref="IDispatchContext.Transport"/>) or for a cancellation request (by token prefix).
/// </summary>
internal interface IScheduledMessageStoreResolver
{
    /// <summary>
    /// Resolves the scheduled-message store for the transport carried by <paramref name="context"/>.
    /// Returns the transport-specific store when one is registered; otherwise the fallback store
    /// when one is registered; otherwise <see langword="false"/>.
    /// </summary>
    bool TryGetForDispatch(IDispatchContext context, out IScheduledMessageStore store);

    /// <summary>
    /// Resolves the scheduled-message store responsible for an opaque cancellation token by
    /// matching the token's <c>provider:</c> prefix against registered stores.
    /// </summary>
    bool TryGetForCancellation(string token, out IScheduledMessageStore store);
}
