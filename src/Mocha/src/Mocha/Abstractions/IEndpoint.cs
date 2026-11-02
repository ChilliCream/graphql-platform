namespace Mocha;

/// <summary>
/// Represents a named messaging endpoint with an addressable URI, serving as the base abstraction
/// for both dispatch and receive endpoints.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Gets the logical name of this endpoint.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the transport-level address URI for this endpoint.
    /// </summary>
    Uri Address { get; }
}
