namespace Mocha;

/// <summary>
/// Holds the resolved configuration for a dispatch endpoint, including its routes and dispatch-scoped middleware pipeline.
/// </summary>
public class DispatchEndpointConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the logical name of the dispatch endpoint.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of dispatch endpoint, controlling how outbound messages are routed.
    /// </summary>
    public DispatchEndpointKind Kind { get; set; } = DispatchEndpointKind.Default;

    /// <summary>
    /// Gets or sets the list of outbound route bindings specifying message types and their routing kind (send or publish).
    /// </summary>
    public List<(Type RuntimeType, OutboundRouteKind Kind)> Routes { get; set; } = [];

    /// <summary>
    /// Gets or sets the dispatch-scoped middleware configurations executed during message dispatch.
    /// </summary>
    public List<DispatchMiddlewareConfiguration> DispatchMiddlewares { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of pipeline modifiers that can reorder or replace dispatch middleware at build time.
    /// </summary>
    public List<Action<List<DispatchMiddlewareConfiguration>>> DispatchPipelineModifiers { get; set; } = [];
}
