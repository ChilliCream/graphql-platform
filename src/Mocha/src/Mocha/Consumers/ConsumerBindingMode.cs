namespace Mocha;

/// <summary>
/// Specifies how a consumer is bound to its inbound route during endpoint discovery.
/// </summary>
public enum ConsumerBindingMode
{
    /// <summary>
    /// The consumer requires an explicit route configuration to bind to an endpoint.
    /// </summary>
    Explicit,

    /// <summary>
    /// The consumer is automatically bound to an endpoint based on naming conventions.
    /// </summary>
    Implicit
}
