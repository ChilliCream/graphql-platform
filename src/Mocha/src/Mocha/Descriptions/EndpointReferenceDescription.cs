namespace Mocha;

/// <summary>
/// Describes a reference to an endpoint within a route, identifying the endpoint by name, address, and owning transport.
/// </summary>
/// <param name="Name">The logical name of the endpoint.</param>
/// <param name="Address">The transport-level address URI, or <c>null</c> if not yet resolved.</param>
/// <param name="TransportName">The name of the transport that owns this endpoint.</param>
public sealed record EndpointReferenceDescription(string Name, string? Address, string TransportName);
