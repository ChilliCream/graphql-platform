namespace Mocha;

/// <summary>
/// Describes a messaging transport for diagnostic and visualization purposes.
/// </summary>
/// <param name="Id">The stable URN identity of this transport.</param>
/// <param name="Address">The base address of the transport topology.</param>
/// <param name="Name">The logical name of the transport.</param>
/// <param name="Schema">The URI scheme used by this transport.</param>
/// <param name="TransportType">The CLR type name of the transport implementation.</param>
/// <param name="ReceiveEndpoints">The receive endpoints owned by this transport.</param>
/// <param name="DispatchEndpoints">The dispatch endpoints owned by this transport.</param>
/// <param name="Topology">The transport-level topology, or <c>null</c> if not available.</param>
public sealed record TransportDescription(
    string Id,
    string Address,
    string Name,
    string Schema,
    string TransportType,
    IReadOnlyList<ReceiveEndpointDescription> ReceiveEndpoints,
    IReadOnlyList<DispatchEndpointDescription> DispatchEndpoints,
    TopologyDescription? Topology);
