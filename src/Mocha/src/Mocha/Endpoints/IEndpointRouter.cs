using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Mocha;

/// <summary>
/// Routes dispatch endpoints by address, supporting lookup, creation, aliasing, and removal of endpoints across transports.
/// </summary>
public interface IEndpointRouter
{
    /// <summary>
    /// Gets a change token that fires when the endpoint topology may have changed.
    /// </summary>
    IChangeToken GetChangeToken();

    /// <summary>
    /// Gets all registered dispatch endpoints.
    /// </summary>
    IReadOnlyList<DispatchEndpoint> Endpoints { get; }

    /// <summary>
    /// Attempts to find a dispatch endpoint registered for the specified address.
    /// </summary>
    /// <param name="address">The address to look up.</param>
    /// <param name="endpoint">The found dispatch endpoint, or <c>null</c> if none matched.</param>
    /// <returns><c>true</c> if an endpoint was found; <c>false</c> otherwise.</returns>
    bool TryGet(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint);

    /// <summary>
    /// Returns all dispatch endpoints registered for the specified address.
    /// </summary>
    /// <param name="address">The address to look up.</param>
    /// <returns>An immutable set of matching endpoints, or an empty set if none matched.</returns>
    ImmutableHashSet<DispatchEndpoint> GetAll(Uri address);

    /// <summary>
    /// Returns an existing dispatch endpoint for the address, or creates one by asking configured transports.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="address">The address to resolve or create an endpoint for.</param>
    /// <returns>The resolved or newly created dispatch endpoint.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no transport can handle the address.</exception>
    DispatchEndpoint GetOrCreate(IMessagingConfigurationContext context, Uri address);

    /// <summary>
    /// Registers or updates a dispatch endpoint and indexes all of its known addresses.
    /// </summary>
    /// <param name="endpoint">The dispatch endpoint to register.</param>
    void AddOrUpdate(DispatchEndpoint endpoint);

    /// <summary>
    /// Adds an additional address alias to an already-registered dispatch endpoint.
    /// </summary>
    /// <param name="endpoint">The dispatch endpoint to add the address to.</param>
    /// <param name="address">The additional address to associate with the endpoint.</param>
    /// <exception cref="InvalidOperationException">Thrown when the endpoint is not yet registered.</exception>
    void AddAddress(DispatchEndpoint endpoint, Uri address);

    /// <summary>
    /// Removes a dispatch endpoint and all of its address associations from the router.
    /// </summary>
    /// <param name="endpoint">The dispatch endpoint to remove.</param>
    void Remove(DispatchEndpoint endpoint);
}
