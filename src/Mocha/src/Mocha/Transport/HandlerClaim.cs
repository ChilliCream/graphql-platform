namespace Mocha;

/// <summary>
/// Stores a transport-level handler claim, capturing the handler type and an optional
/// endpoint configuration action to apply when the claim is materialized.
/// </summary>
internal sealed class HandlerClaim
{
    /// <summary>
    /// Gets the handler type that is claimed by the transport.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// Gets or sets an optional configuration action applied to the receive endpoint descriptor
    /// when the claim is materialized. The delegate accepts the endpoint descriptor as <see cref="object"/>
    /// and casts internally to the transport-specific type.
    /// </summary>
    public Action<object>? ConfigureEndpoint { get; set; }
}
