using Microsoft.Extensions.Primitives;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Supplies an external data document that is merged into the Rego data document used to compile
/// the policy set.
/// </summary>
/// <remarks>
/// A provider is registered through <c>AddRegoDataProvider</c> and is identified by the name it is
/// registered under, not by any property of the provider itself. Implementations must be safe to
/// call concurrently with themselves; the aggregator never issues overlapping calls to the same
/// provider instance, but a provider may still be asked to refresh while a prior call is still
/// completing on a different provider.
/// </remarks>
public interface IRegoDataProvider
{
    /// <summary>
    /// Retrieves the current data snapshot from the provider's source.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that is canceled when the refresh exceeds its allotted timeout.
    /// </param>
    /// <returns>The current data snapshot.</returns>
    ValueTask<RegoDataSnapshot> GetDataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a change token that signals when the provider's data may have changed and should be
    /// refreshed. The aggregator requests a new token every time the previous one fires.
    /// </summary>
    IChangeToken GetChangeToken();
}
